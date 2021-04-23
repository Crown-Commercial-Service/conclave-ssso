using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class OrganisationProfileService : IOrganisationProfileService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly ICiiService _ciiService;
    private readonly IOrganisationService _organisationHelperService;
    public OrganisationProfileService(IDataContext dataContext, IContactsHelperService contactsHelper, ICcsSsoEmailService ccsSsoEmailService,
      ICiiService ciiService, IOrganisationService organisationHelperService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _ccsSsoEmailService = ccsSsoEmailService;
      _ciiService = ciiService;
      _organisationHelperService = organisationHelperService;
    }

    /// <summary>
    /// Create organisation profile and the physical address with OTHER contact reson
    /// </summary>
    /// <param name="organisationProfileInfo"></param>
    /// <returns></returns>
    public async Task<string> CreateOrganisationAsync(OrganisationProfileInfo organisationProfileInfo)
    {
      Validate(organisationProfileInfo);

      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.ExternalOrgnaisation)).Id;
      var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(ContactReasonType.Other);

      var organisation = new Organisation
      {
        CiiOrganisationId = organisationProfileInfo.Detail.OrganisationId,
        LegalName = organisationProfileInfo.Identifier.LegalName.Trim(),
        OrganisationUri = organisationProfileInfo.Identifier.Uri.Trim()
      };

      organisation.IsSme = organisationProfileInfo.Detail.IsSme;
      organisation.IsVcse = organisationProfileInfo.Detail.IsVcse;
      organisation.RightToBuy = organisationProfileInfo.Detail.RightToBuy;
      organisation.IsActivated = organisationProfileInfo.Detail.IsActive;
      organisation.SupplierBuyerType = organisationProfileInfo.Detail.SupplierBuyerType;

      var eligibleRoles = await _organisationHelperService.GetOrganisationEligibleRolesAsync(organisation, organisationProfileInfo.Detail.SupplierBuyerType);
      _dataContext.OrganisationEligibleRole.AddRange(eligibleRoles);

      var eligibleIdentityProviders = new List<OrganisationEligibleIdentityProvider>();
      var identityProviders = await _dataContext.IdentityProvider.Where(idp => !idp.IsDeleted).ToListAsync();
      identityProviders.ForEach((idp) =>
      {
        var eligibleIdentityProvider = new OrganisationEligibleIdentityProvider
        {
          IdentityProvider = idp,
          Organisation = organisation
        };
        eligibleIdentityProviders.Add(eligibleIdentityProvider);
      });
      _dataContext.OrganisationEligibleIdentityProvider.AddRange(eligibleIdentityProviders);

      var contactPoint = new ContactPoint
      {
        PartyTypeId = partyTypeId,
        ContactPointReasonId = contactPointReasonId,
        ContactDetail = new ContactDetail
        {
          EffectiveFrom = DateTime.UtcNow,
          PhysicalAddress = new PhysicalAddress { }
        }
      };

      AssignPhysicalContactsToContactPoint(organisationProfileInfo.Address, contactPoint);

      var party = new Party
      {
        PartyTypeId = partyTypeId,
        Organisation = organisation,
        ContactPoints = new List<ContactPoint>()
      };

      party.ContactPoints.Add(contactPoint);

      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();

      return organisation.CiiOrganisationId;
    }

    //Deletion has commented out since its not going to be exposed via the api at the moment
    //public async Task DeleteOrganisationAsync(string ciiOrganisationId)
    //{
    //  List<string> userNames = new List<string>();

    //  var deletingOrganisation = await _dataContext.Organisation
    //    .Include(o => o.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
    //    .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

    //  if (deletingOrganisation != null)
    //  {
    //    deletingOrganisation.Party.IsDeleted = true;
    //    deletingOrganisation.IsDeleted = true;
    //    if (deletingOrganisation.Party.ContactPoints != null)
    //    {
    //      foreach (var orgContactPoint in deletingOrganisation.Party.ContactPoints)
    //      {
    //        orgContactPoint.IsDeleted = true;
    //        orgContactPoint.ContactDetail.IsDeleted = true;
    //        orgContactPoint.ContactDetail.PhysicalAddress.IsDeleted = true;
    //      }
    //    }

    //    var deletingOrganisationPeople = await _dataContext.Organisation
    //    .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.User)
    //    .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
    //    .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

    //    if (deletingOrganisationPeople.People != null)
    //    {
    //      foreach (var person in deletingOrganisationPeople.People)
    //      {
    //        person.Party.IsDeleted = true;

    //        if (person.Party.User != null)
    //        {
    //          person.Party.User.IsDeleted = true;
    //          userNames.Add(person.Party.User.UserName); // Add the userName to delete from Auth0
    //        }

    //        if (person.Party.ContactPoints != null)
    //        {
    //          foreach (var personContactPoint in person.Party.ContactPoints)
    //          {
    //            personContactPoint.IsDeleted = true;
    //            personContactPoint.ContactDetail.IsDeleted = true;

    //            if (personContactPoint.ContactDetail.VirtualAddresses != null)
    //            {
    //              foreach (var virtualContact in personContactPoint.ContactDetail.VirtualAddresses)
    //              {
    //                virtualContact.IsDeleted = true;
    //              }
    //            }
    //          }
    //        }
    //      }
    //    }

    //    //TODO delete other entities on Organisation

    //    await _dataContext.SaveChangesAsync();

    //  }
    //}

    /// <summary>
    /// Get the prganisation with its physical address
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <returns></returns>
    public async Task<OrganisationProfileResponseInfo> GetOrganisationAsync(string ciiOrganisationId)
    {
      var organisation = await _dataContext.Organisation
        .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
        .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {

        var ciiOrganisation = (await _ciiService.GetOrgsAsync(ciiOrganisationId)).FirstOrDefault();

        if (ciiOrganisation == null)
        {
          throw new ResourceNotFoundException();
        }

        var organisationInfo = new OrganisationProfileResponseInfo
        {
          Identifier = new OrganisationIdentifier
          {
            Id = ciiOrganisation?.identifier?.id ?? string.Empty,
            Scheme = ciiOrganisation?.identifier?.scheme ?? string.Empty,
            LegalName = ciiOrganisation?.identifier?.legalName ?? string.Empty,
            Uri = ciiOrganisation?.identifier?.uri ?? string.Empty,
          },
          Detail = new OrganisationDetail
          {
            OrganisationId = organisation.CiiOrganisationId,
            IsActive = organisation.IsActivated,
            IsSme = organisation.IsSme,
            IsVcse = organisation.IsVcse,
            SupplierBuyerType = organisation.SupplierBuyerType != null ? (int)organisation.SupplierBuyerType : 0,
            CreationDate = organisation.CreatedOnUtc.ToString(DateTimeFormat.DateFormat)
          },
          AdditionalIdentifiers = new List<OrganisationIdentifier>()
        };

        if (organisation.Party.ContactPoints != null)
        {
          var contactPoint = organisation.Party.ContactPoints.OrderBy(cp => cp.CreatedOnUtc).FirstOrDefault();

          if (contactPoint != null)
          {
            var address = new OrganisationAddress
            {
              StreetAddress = contactPoint.ContactDetail.PhysicalAddress.StreetAddress ?? string.Empty,
              Region = contactPoint.ContactDetail.PhysicalAddress.Region ?? string.Empty,
              Locality = contactPoint.ContactDetail.PhysicalAddress.Locality ?? string.Empty,
              PostalCode = contactPoint.ContactDetail.PhysicalAddress.PostalCode ?? string.Empty,
              CountryCode = contactPoint.ContactDetail.PhysicalAddress.CountryCode ?? string.Empty,
            };
            organisationInfo.Address = address;
          }
        }

        if (ciiOrganisation?.additionalIdentifiers != null)
        {
          foreach(var ciiAdditionalIdentifier in ciiOrganisation.additionalIdentifiers)
          {
            var identifier = new OrganisationIdentifier
            {
              Id = ciiAdditionalIdentifier.id,
              Scheme = ciiAdditionalIdentifier.scheme,
              LegalName = ciiAdditionalIdentifier.legalName,
              Uri = string.Empty
            };
            organisationInfo.AdditionalIdentifiers.Add(identifier);
          }
        }

        // Commented since this is still not available from CII service
        //if (ciiOrganisation?.contactPoint != null)
        //{
        //  organisationInfo.ContactPoint = new OrganisationContactPoint
        //  {
        //    Name = ciiOrganisation.contactPoint.name ?? string.Empty,
        //    Email = ciiOrganisation.contactPoint.email ?? string.Empty,
        //    Telephone = ciiOrganisation.contactPoint.telephone ?? string.Empty,
        //    FaxNumber = ciiOrganisation.contactPoint.faxNumber ?? string.Empty,
        //    Uri = ciiOrganisation.contactPoint.uri ?? string.Empty,
        //  };
        //}

        return organisationInfo;
      }

      throw new ResourceNotFoundException();
    }

    public async Task<List<IdentityProviderDetail>> GetOrganisationIdentityProvidersAsync(string ciiOrganisationId)
    {

      var organisation = await _dataContext.Organisation
       .Include(o => o.OrganisationEligibleIdentityProviders).ThenInclude(oi => oi.IdentityProvider)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var identityProviders = organisation.OrganisationEligibleIdentityProviders.Where(x => !x.IsDeleted).Select(i => new IdentityProviderDetail
      {
        Id = i.Id,
        ConnectionName = i.IdentityProvider.IdpConnectionName,
        Name = i.IdentityProvider.IdpName
      }).ToList();

      return identityProviders;
    }

    public async Task<List<OrganisationRole>> GetOrganisationRolesAsync(string ciiOrganisationId)
    {
      var organisation = await _dataContext.Organisation
       .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var roles = organisation.OrganisationEligibleRoles.Where(x => !x.IsDeleted).Select(or => new OrganisationRole
      {
        RoleId = or.Id,
        RoleName = or.CcsAccessRole.CcsAccessRoleName,
        OrgTypeEligibility = or.CcsAccessRole.OrgTypeEligibility,
        SubscriptionTypeEligibility = or.CcsAccessRole.SubscriptionTypeEligibility,
        TradeEligibility = or.CcsAccessRole.TradeEligibility
      }).ToList();

      return roles;
    }

    public async Task<List<OrganisationRole>> GetEligableRolesAsync(string ciiOrganisationId)
    {
      var organisation = await _dataContext.Organisation
      .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
      .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        return new List<OrganisationRole>();
      }

      var roles = organisation.OrganisationEligibleRoles.Where(x => !x.IsDeleted).Select(or => new OrganisationRole
      {
        RoleId = or.Id,
        RoleName = or.CcsAccessRole.CcsAccessRoleName,
        OrgTypeEligibility = or.CcsAccessRole.OrgTypeEligibility,
        SubscriptionTypeEligibility = or.CcsAccessRole.SubscriptionTypeEligibility,
        TradeEligibility = or.CcsAccessRole.TradeEligibility,
      }).ToList();

      return roles;
    }

    public async Task UpdateIdentityProviderAsync(string ciiOrganisationId, string idpName, bool enabled)
    {
      var identityProvider = await _dataContext.IdentityProvider.FirstOrDefaultAsync(x => x.IsDeleted == false && x.IdpConnectionName.Equals(idpName));
      var organisationIdps = await _dataContext.OrganisationEligibleIdentityProvider
        .Include(o => o.Organisation)
        .Include(o => o.IdentityProvider)
        .Where(x => x.Organisation.CiiOrganisationId == ciiOrganisationId && x.Organisation.IsDeleted == false)
        .ToListAsync();
      var organisation = await _dataContext.Organisation
        .Where(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId)
        .FirstOrDefaultAsync();

      if (organisationIdps.Any())
      {
        organisationIdps.ForEach((idp) =>
        {
          if (idp.IdentityProvider.IdpConnectionName == idpName)
          {
            idp.IsDeleted = !enabled;
            if (enabled == false)
            {
              var noneIdentityProvider = _dataContext.OrganisationEligibleIdentityProvider
              .Include(o => o.IdentityProvider)
              .FirstOrDefault(x => x.IsDeleted == false && x.IdentityProvider.IdpConnectionName == "none");
              var users = _dataContext.User
                .Include(o => o.OrganisationEligibleIdentityProvider)
                .Where(x => x.OrganisationEligibleIdentityProvider.Id == idp.Id)
                .ToList();
              if (users.Any() && noneIdentityProvider != null)
              {
                users.ForEach((u) =>
                {
                  u.OrganisationEligibleIdentityProvider = noneIdentityProvider;
                });
              }
            }
          }
        });
        await _dataContext.SaveChangesAsync();
      }
      else
      {
        var e = new OrganisationEligibleIdentityProvider
        {
          IdentityProvider = identityProvider,
          Organisation = organisation
        };
        _dataContext.OrganisationEligibleIdentityProvider.Add(e);
        await _dataContext.SaveChangesAsync();
      }
    }
    /// <summary>
    /// Update the organisation and its physical address
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="organisationProfileInfo"></param>
    /// <returns></returns>
    public async Task UpdateOrganisationAsync(string ciiOrganisationId, OrganisationProfileInfo organisationProfileInfo)
    {
      Validate(organisationProfileInfo);

      var organisation = await _dataContext.Organisation
       .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
       .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        organisation.LegalName = organisationProfileInfo.Identifier.LegalName.Trim();
        organisation.OrganisationUri = organisationProfileInfo.Identifier.Uri.Trim();

        if (organisationProfileInfo.Detail != null)
        {
          organisation.IsSme = organisationProfileInfo.Detail.IsSme;
          organisation.IsVcse = organisationProfileInfo.Detail.IsVcse;
          organisation.RightToBuy = organisationProfileInfo.Detail.RightToBuy;
          organisation.IsActivated = organisationProfileInfo.Detail.IsActive;
        }

        if (organisationProfileInfo.Address != null && organisation.Party.ContactPoints != null)
        {
          var physicalAddressContactPoint = organisation.Party.ContactPoints.OrderBy(cp => cp.CreatedOnUtc).FirstOrDefault();

          if (physicalAddressContactPoint != null)
          {
            AssignPhysicalContactsToContactPoint(organisationProfileInfo.Address, physicalAddressContactPoint);
          }
        }

        await _dataContext.SaveChangesAsync();

        // Get org admins and send emails
        var orgAdmins = await GetOrganisationAdmins(organisation.Id);
        foreach (var admin in orgAdmins)
        {
          await _ccsSsoEmailService.SendOrgProfileUpdateEmailAsync(admin.UserName);
        }
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    public async Task<List<User>> GetOrganisationAdmins(int organisationId)
    {
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
       .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId
       && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey))?.Id;

      var orgAdmins = await _dataContext.User.Where(u => !u.IsDeleted &&
       u.Party.Person.OrganisationId == organisationId &&
       (u.UserGroupMemberships.Any(ugm => !ugm.IsDeleted &&
       ugm.OrganisationUserGroup.GroupEligibleRoles.Any(ga => !ga.IsDeleted && ga.OrganisationEligibleRoleId == orgAdminAccessRoleId)) ||
        u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId)))
      .Select(u => u).ToListAsync();

      return orgAdmins;
    }

    /// <summary>
    /// Add/Delete OrgElegibleRoles
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="isBuyer"></param>
    /// <param name="rolesToAdd"></param>
    /// <param name="rolesToDelete"></param>
    /// <returns></returns>
    public async Task UpdateOrganisationAsync(string ciiOrganisationId, bool isBuyer, List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete)
    {
      var organisation = await _dataContext.Organisation
        .Include(er => er.OrganisationEligibleRoles)
        .ThenInclude(r => r.CcsAccessRole)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        organisation.RightToBuy = isBuyer;

        if (rolesToAdd != null)
        {
          rolesToAdd.ForEach(async (x) => {
            var roleEntity = _dataContext.CcsAccessRole.FirstOrDefault(r => !r.IsDeleted && r.Id == x.RoleId);
            if (roleEntity != null)
            {
              _dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole
              {
                Organisation = organisation,
                CcsAccessRole = roleEntity
              });

            }
          });
        }
        if (rolesToDelete != null)
        {
          rolesToDelete.ForEach(async (x) =>
          {
            var role = organisation.OrganisationEligibleRoles.FirstOrDefault(r => !r.IsDeleted && r.CcsAccessRoleId == x.RoleId);
            if (role != null)
            {
              role.IsDeleted = true;

              //var groupsUgm = await _dataContext.UserGroupMembership
              //  .Include(i => i.OrganisationUserGroup)
              //  .ThenInclude(i => i.GroupEligibleRoles)
              //  .ThenInclude(i => i.OrganisationEligibleRole)
              //  .ThenInclude(i => i.CcsAccessRole)
              //  .Where(u => u.OrganisationUserGroup.OrganisationId == organisation.Id)
              //  .SelectMany(r => r.OrganisationUserGroup.GroupEligibleRoles.Where(d => d.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == role.CcsAccessRole.CcsAccessRoleNameKey))
              //  .ToListAsync();

              //groupsUgm.ForEach(async (g) =>
              //{
              //  g.IsDeleted = true;
              //});

              // Groups
              var groupsOger = await _dataContext.OrganisationGroupEligibleRole
              .Include(i => i.OrganisationEligibleRole)
              .ThenInclude(i => i.CcsAccessRole)
              .Where(r => r.OrganisationEligibleRole.OrganisationId == organisation.Id && r.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == role.CcsAccessRole.CcsAccessRoleNameKey)
              .ToListAsync();

              groupsOger.ForEach(async (g) =>
              {
                g.IsDeleted = true;
              });

              // Users
              var usersOger = await _dataContext.UserAccessRole
              .Include(i => i.OrganisationEligibleRole)
              .ThenInclude(i => i.CcsAccessRole)
              .Where(r => r.OrganisationEligibleRole.OrganisationId == organisation.Id && r.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == role.CcsAccessRole.CcsAccessRoleNameKey)
              .ToListAsync();

              usersOger.ForEach(async (g) =>
              {
                g.IsDeleted = true;
              });
            }
          });
        }

        await _dataContext.SaveChangesAsync();
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    private void AssignPhysicalContactsToContactPoint(OrganisationAddress addressDetails, ContactPoint contactPoint)
    {
      contactPoint.ContactDetail.PhysicalAddress.StreetAddress = addressDetails.StreetAddress;
      contactPoint.ContactDetail.PhysicalAddress.Region = addressDetails.Region;
      contactPoint.ContactDetail.PhysicalAddress.Locality = addressDetails.Locality;
      contactPoint.ContactDetail.PhysicalAddress.PostalCode = addressDetails.PostalCode;
      contactPoint.ContactDetail.PhysicalAddress.CountryCode = addressDetails.CountryCode;
    }

    private void Validate(OrganisationProfileInfo organisationProfileInfo)
    {

      if (organisationProfileInfo.Detail == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      if (string.IsNullOrWhiteSpace(organisationProfileInfo.Detail.OrganisationId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      if (organisationProfileInfo.Identifier == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidIdentifier);
      }

      if (string.IsNullOrWhiteSpace(organisationProfileInfo.Identifier.LegalName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidOrganisationName);
      }

      if (string.IsNullOrWhiteSpace(organisationProfileInfo.Identifier.Uri))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidOrganisationUri);
      }

      // TODO - finalize this in the Org wrapper review
      //if (organisationProfileInfo.Address == null || (string.IsNullOrWhiteSpace(organisationProfileInfo.Address.StreetAddress) && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.Locality)
      //  && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.Region) && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.PostalCode)
      //  && string.IsNullOrWhiteSpace(organisationProfileInfo.Address.CountryCode)))
      //{
      //  throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
      //}
    }

  }
}
