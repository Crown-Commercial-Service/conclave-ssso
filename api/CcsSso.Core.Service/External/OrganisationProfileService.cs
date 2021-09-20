using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly IAdaptorNotificationService _adapterNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly ILocalCacheService _localCacheService;
    public OrganisationProfileService(IDataContext dataContext, IContactsHelperService contactsHelper, ICcsSsoEmailService ccsSsoEmailService,
      ICiiService ciiService, IAdaptorNotificationService adapterNotificationService,
      IWrapperCacheService wrapperCacheService, ILocalCacheService localCacheService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _ccsSsoEmailService = ccsSsoEmailService;
      _ciiService = ciiService;
      _adapterNotificationService = adapterNotificationService;
      _wrapperCacheService = wrapperCacheService;
      _localCacheService = localCacheService;
    }

    /// <summary>
    /// Create organisation profile and the physical address with OTHER contact reson
    /// </summary>
    /// <param name="organisationProfileInfo"></param>
    /// <returns></returns>
    public async Task<string> CreateOrganisationAsync(OrganisationProfileInfo organisationProfileInfo)
    {
      Validate(organisationProfileInfo);

      if (await _dataContext.Organisation.AnyAsync(org => !org.IsDeleted && org.CiiOrganisationId == organisationProfileInfo.Detail.OrganisationId))
      {
        throw new ResourceAlreadyExistsException();
      }

      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.ExternalOrgnaisation)).Id;
      var contactPointReasonId = await _contactsHelper.GetContactPointReasonIdAsync(ContactReasonType.Other);

      var organisation = new Organisation
      {
        CiiOrganisationId = organisationProfileInfo.Detail.OrganisationId,
        LegalName = organisationProfileInfo.Identifier.LegalName.Trim(),
        OrganisationUri = organisationProfileInfo.Identifier.Uri?.Trim()
      };

      organisation.IsSme = organisationProfileInfo.Detail.IsSme;
      organisation.IsVcse = organisationProfileInfo.Detail.IsVcse;
      organisation.RightToBuy = organisationProfileInfo.Detail.RightToBuy;
      organisation.IsActivated = organisationProfileInfo.Detail.IsActive;
      organisation.SupplierBuyerType = organisationProfileInfo.Detail.SupplierBuyerType;
      organisation.BusinessType = organisationProfileInfo.Detail.BusinessType;

      var eligibleRoles = await GetOrganisationEligibleRolesAsync(organisation, organisationProfileInfo.Detail.SupplierBuyerType);
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

      if (organisationProfileInfo.Address != null)
      {
        AssignPhysicalContactsToContactPoint(organisationProfileInfo.Address, contactPoint);
      }

      var party = new Party
      {
        PartyTypeId = partyTypeId,
        Organisation = organisation,
        ContactPoints = new List<ContactPoint>()
      };

      party.ContactPoints.Add(contactPoint);

      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();

      // Notify the adapter
      await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Create, organisation.CiiOrganisationId);

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

        var ciiOrganisation = (await _ciiService.GetOrgsAsync(ciiOrganisationId, "")).FirstOrDefault();

        if (ciiOrganisation == null)
        {
          throw new ResourceNotFoundException();
        }

        var organisationInfo = new OrganisationProfileResponseInfo
        {
          Identifier = new OrganisationIdentifier
          {
            Id = ciiOrganisation?.Identifier?.Id ?? string.Empty,
            Scheme = ciiOrganisation?.Identifier?.Scheme ?? string.Empty,
            LegalName = ciiOrganisation?.Identifier?.LegalName ?? string.Empty,
            Uri = ciiOrganisation?.Identifier?.Uri ?? string.Empty,
          },
          Detail = new OrganisationDetail
          {
            OrganisationId = organisation.CiiOrganisationId,
            IsActive = organisation.IsActivated,
            IsSme = organisation.IsSme,
            IsVcse = organisation.IsVcse,
            RightToBuy = organisation.RightToBuy ?? false,
            SupplierBuyerType = organisation.SupplierBuyerType != null ? (int)organisation.SupplierBuyerType : 0,
            BusinessType = organisation.BusinessType ?? string.Empty,
            CreationDate = organisation.CreatedOnUtc.ToString(DateTimeFormat.DateFormat)
          },
          AdditionalIdentifiers = new List<OrganisationIdentifier>()
        };

        if (organisation.Party.ContactPoints != null)
        {
          var contactPoint = organisation.Party.ContactPoints.OrderBy(cp => cp.CreatedOnUtc).FirstOrDefault();

          if (contactPoint != null)
          {
            var address = new OrganisationAddressResponse
            {
              StreetAddress = contactPoint.ContactDetail.PhysicalAddress.StreetAddress ?? string.Empty,
              Region = contactPoint.ContactDetail.PhysicalAddress.Region ?? string.Empty,
              Locality = contactPoint.ContactDetail.PhysicalAddress.Locality ?? string.Empty,
              PostalCode = contactPoint.ContactDetail.PhysicalAddress.PostalCode ?? string.Empty,
              CountryCode = contactPoint.ContactDetail.PhysicalAddress.CountryCode ?? string.Empty,
            };

            if (!string.IsNullOrEmpty(contactPoint.ContactDetail.PhysicalAddress?.CountryCode))
            {
              address.CountryName = CultureSupport.GetCountryNameByCode(contactPoint.ContactDetail.PhysicalAddress.CountryCode);
            }
            organisationInfo.Address = address;
          }
        }

        if (ciiOrganisation?.AdditionalIdentifiers != null)
        {
          foreach (var ciiAdditionalIdentifier in ciiOrganisation.AdditionalIdentifiers)
          {
            var identifier = new OrganisationIdentifier
            {
              Id = ciiAdditionalIdentifier.Id,
              Scheme = ciiAdditionalIdentifier.Scheme,
              LegalName = ciiAdditionalIdentifier.LegalName,
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

      var identityProviders = organisation.OrganisationEligibleIdentityProviders.Where(x => !x.IsDeleted)
        .OrderBy(idp => idp.IdentityProviderId).Select(i => new IdentityProviderDetail
        {
          Id = i.Id,
          ConnectionName = i.IdentityProvider.IdpConnectionName,
          Name = i.IdentityProvider.IdpName
        }).ToList();

      return identityProviders;
    }

    public async Task<List<OrganisationRole>> GetOrganisationRolesAsync(string ciiOrganisationId)
    {
      // Read org table to find the Org and then include all roles (FirstOrDefaultAsync get the Org)
      var orgEligibleRoles = await _dataContext.Organisation
       .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
        .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
        .Where(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId)
        .Select(o => o.OrganisationEligibleRoles)
       .FirstOrDefaultAsync();

      if (orgEligibleRoles == null)
      {
        throw new ResourceNotFoundException();
      }

      var roles = orgEligibleRoles.Where(x => !x.IsDeleted)
        .OrderBy(r => r.CcsAccessRoleId).Select(or => new OrganisationRole
        {
          RoleId = or.Id,
          RoleName = or.CcsAccessRole.CcsAccessRoleName,
          ServiceName = or.CcsAccessRole?.ServiceRolePermissions?.FirstOrDefault()?.ServicePermission.CcsService.ServiceName,
          OrgTypeEligibility = or.CcsAccessRole.OrgTypeEligibility,
          SubscriptionTypeEligibility = or.CcsAccessRole.SubscriptionTypeEligibility,
          TradeEligibility = or.CcsAccessRole.TradeEligibility
        }).ToList();

      return roles;
    }

    public async Task UpdateIdentityProviderAsync(OrgIdentityProviderSummary orgIdentityProviderSummary)
    {
      if (orgIdentityProviderSummary.ChangedOrgIdentityProviders != null && orgIdentityProviderSummary.ChangedOrgIdentityProviders.Any() && !string.IsNullOrEmpty(orgIdentityProviderSummary.CiiOrganisationId))
      {
        var organisation = await _dataContext.Organisation
                                .Where(o => !o.IsDeleted && o.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId)
                                .FirstOrDefaultAsync();

        if (organisation != null)
        {
          // This will include all idps including "none"
          var identityProviderList = await _dataContext.IdentityProvider.Where(idp => !idp.IsDeleted).ToListAsync();

          var organisationIdps = await _dataContext.OrganisationEligibleIdentityProvider
            .Include(o => o.IdentityProvider)
            .Where(oeidp => !oeidp.IsDeleted && oeidp.Organisation.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId)
            .ToListAsync();

          var noneOrgIdentityProvider = organisationIdps.FirstOrDefault(ip => ip.IdentityProvider.IdpConnectionName == "none");

          foreach (var idp in orgIdentityProviderSummary.ChangedOrgIdentityProviders.Where(ip => identityProviderList.Select(tl => tl.Id).Contains(ip.Id)))
          {
            if (!idp.Enabled)
            {
              //delete the idp provider from the list
              var orgIdpToDelete = organisationIdps.FirstOrDefault(oidp => oidp.IdentityProvider.Id == idp.Id);
              orgIdpToDelete.IsDeleted = true;

              //Update the users to none
              var users = await _dataContext.User
                    .Where(u => u.OrganisationEligibleIdentityProvider.IdentityProviderId == idp.Id)
                    .ToListAsync();
              users.ForEach((u) =>
              {
                //u.OrganisationEligibleIdentityProvider.IdentityProviderId = noneIdentityProvider.Id;
                u.OrganisationEligibleIdentityProviderId = noneOrgIdentityProvider.Id;
              });

              //Invalidate redis
              var invalidatingCacheKeyList = users.Select(u => $"{CacheKeyConstant.User}-{u.UserName}").ToArray();
              await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeyList);

              // Notify the adapter
              var notifyTaskList = new List<Task>();
              users.ForEach((u) =>
              {
                notifyTaskList.Add(_adapterNotificationService.NotifyUserChangeAsync(OperationType.Update, u.UserName, organisation.CiiOrganisationId));
              });
              await Task.WhenAll(notifyTaskList);
            }
            else
            {
              var addedOrganisationEligibleIdentityProvider = new OrganisationEligibleIdentityProvider
              {
                OrganisationId = organisation.Id,
                IdentityProviderId = idp.Id
              };
              _dataContext.OrganisationEligibleIdentityProvider.Add(addedOrganisationEligibleIdentityProvider);
            }
          }
          await _dataContext.SaveChangesAsync();
        }
        else
        {
          throw new ResourceNotFoundException();
        }
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
        organisation.OrganisationUri = organisationProfileInfo.Identifier.Uri?.Trim();

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

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.Organisation}-{ciiOrganisationId}");

        // Notify the adapter
        await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Update, ciiOrganisationId);

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
    public async Task UpdateOrganisationEligibleRolesAsync(string ciiOrganisationId, bool isBuyer, List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete)
    {
      var organisation = await _dataContext.Organisation
        .Include(er => er.OrganisationEligibleRoles)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        organisation.RightToBuy = isBuyer;

        if (rolesToAdd != null && rolesToAdd.Any())
        {
          var ccsAccessRoles = await _dataContext.CcsAccessRole.ToListAsync();

          if (!rolesToAdd.All(ar => ccsAccessRoles.Any(r => r.Id == ar.RoleId)))
          {
            throw new CcsSsoException("INVALID_ROLES_TO_ADD");
          }

          if (rolesToAdd.Any(ar => organisation.OrganisationEligibleRoles.Any(oer => oer.CcsAccessRoleId == ar.RoleId)))
          {
            throw new CcsSsoException("ROLE_ALREADY_EXISTS_FOR_ORGANISATION");
          }

          List<OrganisationEligibleRole> addedEligibleRoles = new List<OrganisationEligibleRole>();

          rolesToAdd.ForEach((addedRole) =>
          {
            addedEligibleRoles.Add(new OrganisationEligibleRole
            {
              OrganisationId = organisation.Id,
              CcsAccessRoleId = addedRole.RoleId
            });
          });
          _dataContext.OrganisationEligibleRole.AddRange(addedEligibleRoles);
        }
        if (rolesToDelete != null && rolesToDelete.Any())
        {
          var deletingRoleIds = rolesToDelete.Select(r => r.RoleId).ToList();

          if (!deletingRoleIds.All(dr => organisation.OrganisationEligibleRoles.Any(oer => !oer.IsDeleted && oer.CcsAccessRoleId == dr)))
          {
            throw new CcsSsoException("INVALID_ROLES_TO_DELETE");
          }

          var deletingOrgEligibleRoles = organisation.OrganisationEligibleRoles.Where(oer => deletingRoleIds.Contains(oer.CcsAccessRoleId)).ToList();

          var orgGroupRolesWithDeletedRoles = await _dataContext.OrganisationGroupEligibleRole
            .Where(oger => !oger.IsDeleted && oger.OrganisationEligibleRole.OrganisationId == organisation.Id && deletingRoleIds.Contains(oger.OrganisationEligibleRole.CcsAccessRoleId))
            .ToListAsync();

          var userAccessRolesWithDeletedRoles = await _dataContext.UserAccessRole
            .Where(uar => !uar.IsDeleted && uar.OrganisationEligibleRole.OrganisationId == organisation.Id && deletingRoleIds.Contains(uar.OrganisationEligibleRole.CcsAccessRoleId))
            .ToListAsync();

          deletingOrgEligibleRoles.ForEach((deletingOrgEligibleRole) =>
          {
            deletingOrgEligibleRole.IsDeleted = true;
          });

          orgGroupRolesWithDeletedRoles.ForEach((orgGroupRolesWithDeletedRole) =>
          {
            orgGroupRolesWithDeletedRole.IsDeleted = true;
          });

          userAccessRolesWithDeletedRoles.ForEach((userAccessRolesWithDeletedRole) =>
          {
            userAccessRolesWithDeletedRole.IsDeleted = true;
          });
        }

        await _dataContext.SaveChangesAsync();

        // Remove service client id inmemory cache since role update
        _localCacheService.Remove($"ORGANISATION_SERVICE_CLIENT_IDS-{ciiOrganisationId}");
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

      if (organisationProfileInfo.Address != null) // Address is not mandatory for an organisation
      {
        if (string.IsNullOrWhiteSpace(organisationProfileInfo.Address.StreetAddress) || string.IsNullOrWhiteSpace(organisationProfileInfo.Address.PostalCode))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInsufficientDetails);
        }

        if (!string.IsNullOrWhiteSpace(organisationProfileInfo.Address.CountryCode) && !CultureSupport.IsValidCountryCode(organisationProfileInfo.Address.CountryCode))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidCountryCode);
        }
      }
    }

    private async Task<List<OrganisationEligibleRole>> GetOrganisationEligibleRolesAsync(Organisation org, int supplierBuyerType)
    {
      var eligibleRoles = new List<OrganisationEligibleRole>();
      if (supplierBuyerType == 0)
      {
        var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Supplier || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToListAsync();
        roles.ForEach((role) =>
        {
          var eligibleRole = new OrganisationEligibleRole
          {
            CcsAccessRole = role,
            Organisation = org,
            MfaEnabled = role.MfaEnabled
          };
          eligibleRoles.Add(eligibleRole);
        });
      }
      else if (supplierBuyerType == 1)
      {
        var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Buyer || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToListAsync();
        roles.ForEach((role) =>
        {
          var eligibleRole = new OrganisationEligibleRole
          {
            CcsAccessRole = role,
            Organisation = org,
            MfaEnabled = role.MfaEnabled
          };
          eligibleRoles.Add(eligibleRole);
        });
      }
      else
      {
        var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Supplier || ar.TradeEligibility == RoleEligibleTradeType.Buyer || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToListAsync();
        roles.ForEach((role) =>
        {
          var eligibleRole = new OrganisationEligibleRole
          {
            CcsAccessRole = role,
            Organisation = org,
            MfaEnabled = role.MfaEnabled
          };
          eligibleRoles.Add(eligibleRole);
        });
      }

      return eligibleRoles;
    }

  }
}
