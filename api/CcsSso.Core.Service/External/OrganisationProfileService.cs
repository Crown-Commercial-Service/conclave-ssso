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
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public partial class OrganisationProfileService : IOrganisationProfileService
  {
    private readonly IDataContext _dataContext;
    private readonly IContactsHelperService _contactsHelper;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly ICiiService _ciiService;
    private readonly IAdaptorNotificationService _adapterNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly ILocalCacheService _localCacheService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly RequestContext _requestContext;
    private readonly IIdamService _idamService;
    private readonly IRemoteCacheService _remoteCacheService;
    private readonly ILookUpService _lookUpService;
    private readonly IOrganisationAuditService _organisationAuditService;
    private readonly IOrganisationAuditEventService _organisationAuditEventService;

    public OrganisationProfileService(IDataContext dataContext, IContactsHelperService contactsHelper, ICcsSsoEmailService ccsSsoEmailService,
      ICiiService ciiService, IAdaptorNotificationService adapterNotificationService,
      IWrapperCacheService wrapperCacheService, ILocalCacheService localCacheService,
      ApplicationConfigurationInfo applicationConfigurationInfo, RequestContext requestContext, IIdamService idamService, IRemoteCacheService remoteCacheService,
      ILookUpService lookUpService, IOrganisationAuditService organisationAuditService, IOrganisationAuditEventService organisationAuditEventService)
    {
      _dataContext = dataContext;
      _contactsHelper = contactsHelper;
      _ccsSsoEmailService = ccsSsoEmailService;
      _ciiService = ciiService;
      _adapterNotificationService = adapterNotificationService;
      _wrapperCacheService = wrapperCacheService;
      _localCacheService = localCacheService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _requestContext = requestContext;
      _idamService = idamService;
      _remoteCacheService = remoteCacheService;
      _lookUpService = lookUpService;
      _organisationAuditService = organisationAuditService;
      _organisationAuditEventService = organisationAuditEventService;
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
      organisation.CcsServiceId = _requestContext.ServiceId == 0 ? null : _requestContext.ServiceId;

      var eligibleRoles = await GetOrganisationEligibleRolesAsync(organisation, organisationProfileInfo.Detail.SupplierBuyerType);
      // #Auto validation enabled then only org admin and user roles will be assigned
      if (_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        eligibleRoles = eligibleRoles.Where(r => r.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey ||
                                                 r.CcsAccessRole.CcsAccessRoleNameKey == Contstant.DefaultUserRoleNameKey).ToList();
      }
      _dataContext.OrganisationEligibleRole.AddRange(eligibleRoles);

      var eligibleIdentityProviders = new List<OrganisationEligibleIdentityProvider>();
      var identityProviders = await _dataContext.IdentityProvider.Where(idp => !idp.IsDeleted && idp.ExternalIdpFlag == false).ToListAsync();
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

      if (!string.IsNullOrEmpty(_requestContext.ServiceClientId))
      {
        var service = await _dataContext.CcsService.FirstOrDefaultAsync(s => s.ServiceClientId == _requestContext.ServiceClientId);

        // #Auto validation
        if (!service.GlobalLevelOrganisationAccess && !_applicationConfigurationInfo.OrgAutoValidation.Enable)
        {
          var eligibleRolesForService = eligibleRoles.Where(oer => _applicationConfigurationInfo.ServiceDefaultRoleInfo.ScopedServiceDefaultRoles.Contains(oer.CcsAccessRole.CcsAccessRoleNameKey)).ToList();
          foreach (var eligibleRole in eligibleRolesForService)
          {
            ExternalServiceRoleMapping externalServiceRoleMapping = new ExternalServiceRoleMapping()
            {
              CcsServiceId = service.Id,
              OrganisationEligibleRole = eligibleRole
            };
            _dataContext.ExternalServiceRoleMapping.Add(externalServiceRoleMapping);
          }
        }
        organisation.IsActivated = service.ActivateOrganisations;
      }

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
        var ciiOrganisation = await _ciiService.GetOrgDetailsAsync(ciiOrganisationId);

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
              address.CountryName = GetCountryNameByCode(contactPoint.ContactDetail.PhysicalAddress?.CountryCode);
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
          RoleKey = or.CcsAccessRole.CcsAccessRoleNameKey,
          RoleName = or.CcsAccessRole.CcsAccessRoleName,
          ServiceName = or.CcsAccessRole?.ServiceRolePermissions?.FirstOrDefault()?.ServicePermission.CcsService.ServiceName,
          OrgTypeEligibility = or.CcsAccessRole.OrgTypeEligibility,
          SubscriptionTypeEligibility = or.CcsAccessRole.SubscriptionTypeEligibility,
          TradeEligibility = or.CcsAccessRole.TradeEligibility
        }).ToList();

      return roles;
    }

    private async Task<List<User>> GetAffectedUsersByRemovedIdp(string ciiOrganisationId, List<int> idpRemovedList)
    {
      return await _dataContext.User.Include(u => u.UserIdentityProviders).ThenInclude(o => o.OrganisationEligibleIdentityProvider)
                          .Include(u => u.Party).ThenInclude(p => p.Person)
                          .Where(u => !u.IsDeleted &&
                          u.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId
                          && u.UserIdentityProviders.Any(uip => !uip.IsDeleted &&
                          idpRemovedList.Contains(uip.OrganisationEligibleIdentityProvider.IdentityProviderId))).ToListAsync();
    }
    private async Task<List<User>> GetOrganisationUser(string ciiOrganisationId)
    {
      return await _dataContext.User.Include(u => u.UserIdentityProviders).ThenInclude(o => o.OrganisationEligibleIdentityProvider)
                          .Include(u => u.Party).ThenInclude(p => p.Person)
                          .Where(u => !u.IsDeleted &&
                          u.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId).ToListAsync();
    }
    //public async Task UpdateIdentityProviderAsync_1(OrgIdentityProviderSummary orgIdentityProviderSummary)
    //{
    //  if (orgIdentityProviderSummary.ChangedOrgIdentityProviders != null && orgIdentityProviderSummary.ChangedOrgIdentityProviders.Any()
    //    && !string.IsNullOrEmpty(orgIdentityProviderSummary.CiiOrganisationId))
    //  {
    //    var organisation = await _dataContext.Organisation
    //                            .Where(o => !o.IsDeleted && o.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId)
    //                            .FirstOrDefaultAsync();

    //    if (organisation != null)
    //    {
    //      // This will include all idps including "none"
    //      var identityProviderList = await _dataContext.IdentityProvider.Where(idp => !idp.IsDeleted).ToListAsync();

    //      var organisationIdps = await _dataContext.OrganisationEligibleIdentityProvider
    //        .Include(o => o.IdentityProvider)
    //        .Where(oeidp => !oeidp.IsDeleted && oeidp.Organisation.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId)
    //        .ToListAsync();

    //      var userNamePasswordIdentityProvider = organisationIdps.FirstOrDefault(ip => ip.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);

    //      var idpList = orgIdentityProviderSummary.ChangedOrgIdentityProviders.Where(ip => identityProviderList.Select(tl => tl.Id).Contains(ip.Id));

    //      //delete the idp provider from the list
    //      var idpRemovedList = idpList.Where(idp => !idp.Enabled).Select(r => r.Id).ToList();

    //      if (idpRemovedList.Contains(userNamePasswordIdentityProvider.Id))
    //      {
    //        throw new CcsSsoException("ERROR_USERNAME_PASSWORD_IDP_REQUIRED");
    //      }

    //      var orgIdpsToDelete = organisationIdps.Where(oidp => idpRemovedList.Contains(oidp.IdentityProvider.Id)).ToList();

    //      orgIdpsToDelete.ForEach((d) =>
    //      {
    //        d.IsDeleted = true;
    //      });

    //      var users = await _dataContext.User.Include(u => u.UserIdentityProviders).ThenInclude(o => o.OrganisationEligibleIdentityProvider)
    //                          .Include(u => u.Party).ThenInclude(p => p.Person)
    //                          .Where(u => !u.IsDeleted &&
    //                          u.Party.Person.Organisation.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId
    //                          && u.UserIdentityProviders.Any(uip => !uip.IsDeleted &&
    //                          idpRemovedList.Contains(uip.OrganisationEligibleIdentityProvider.IdentityProviderId))).ToListAsync();

    //      var asyncTaskList = new List<Task>();
    //      var securityApiCallTaskList = new List<Task>();
    //      users.ForEach((user) =>
    //      {
    //        asyncTaskList.Add(_adapterNotificationService.NotifyUserChangeAsync(OperationType.Update, user.UserName, organisation.CiiOrganisationId));


    //        // Delete before add the record
    //        var recordsToDelete = user.UserIdentityProviders.Where(uip => !uip.IsDeleted && idpRemovedList.Select(i => i).Contains(uip.OrganisationEligibleIdentityProvider.IdentityProviderId)).ToList();
    //        foreach (var uidp in recordsToDelete)
    //        {
    //          uidp.IsDeleted = true;
    //        }

    //        var availableIdps = user.UserIdentityProviders.Where(uidp => !uidp.IsDeleted).Select(uip => uip.OrganisationEligibleIdentityProvider.IdentityProviderId).Except(idpRemovedList.Select(i => i));

    //        if (!availableIdps.Any())
    //        {
    //          SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
    //          {
    //            Email = user.UserName,
    //            FirstName = user.Party.Person.FirstName,
    //            LastName = user.Party.Person.LastName,
    //            UserName = user.UserName,
    //            MfaEnabled = false, //As per the requirement
    //            SendUserRegistrationEmail = true
    //          };
    //          asyncTaskList.Add(_idamService.RegisterUserInIdamAsync(securityApiUserInfo));
    //          user.UserIdentityProviders.Add(new UserIdentityProvider()
    //          {
    //            UserId = user.Id,
    //            OrganisationEligibleIdentityProviderId = userNamePasswordIdentityProvider.Id,
    //            IsDeleted = false
    //          });
    //        }
    //        //Record for force signout as idp has been removed from the user. This is a current business requirement
    //        asyncTaskList.Add(_remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + user.UserName, true));
    //      });

    //      foreach (var idp in idpList.Where(idp => idp.Enabled))
    //      {
    //        var addedOrganisationEligibleIdentityProvider = new OrganisationEligibleIdentityProvider
    //        {
    //          OrganisationId = organisation.Id,
    //          IdentityProviderId = idp.Id
    //        };
    //        _dataContext.OrganisationEligibleIdentityProvider.Add(addedOrganisationEligibleIdentityProvider);
    //      }

    //      await _dataContext.SaveChangesAsync();

    //      await Task.WhenAll(asyncTaskList);
    //      //Invalidate redis
    //      var invalidatingCacheKeyList = users.Select(u => $"{CacheKeyConstant.User}-{u.UserName}").ToArray();
    //      await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeyList);

    //      // Notify the adapter
    //      await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Update, organisation.CiiOrganisationId);
    //    }
    //    else
    //    {
    //      throw new ResourceNotFoundException();
    //    }
    //  }
    //}

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

          if (rolesToAdd.Any(ar => organisation.OrganisationEligibleRoles.Any(oer => !oer.IsDeleted && oer.CcsAccessRoleId == ar.RoleId)))
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

      if (!ValidateCiiOrganisationID(organisationProfileInfo.Detail.OrganisationId))
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

        string CountryName = String.Empty;
        if (!string.IsNullOrEmpty(organisationProfileInfo.Address.CountryCode))
        {
          CountryName = GetCountryNameByCode(organisationProfileInfo.Address.CountryCode);
        }
        if (!string.IsNullOrWhiteSpace(organisationProfileInfo.Address.CountryCode) && string.IsNullOrWhiteSpace(CountryName))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidCountryCode);
        }
      }
    }

    /// <summary>
    /// Validate organisationId
    /// </summary>
    /// <returns></returns>
    public bool ValidateCiiOrganisationID(string CIIOrgID)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(CIIOrgID)) //OrgID mandatory
        {
          return false;
        }
        else if (CIIOrgID.Length != 18) // 18 Digits long
        {
          return false;
        }
        else if (CIIOrgID.StartsWith("0")) //No starting 0's
        {
          return false;
        }
        else if (!CIIOrgID.All(char.IsDigit)) //All characters are numbers 
        {
          return false;
        }
        return true;
      }
      catch (ArgumentException)
      {
      }
      return false;
    }

    /// <summary>
    /// Retrieves CountryName based on country code
    /// </summary>
    /// <returns></returns>
    public string GetCountryNameByCode(string countyCode)
    {
      try
      {
        string CountryName = string.Empty;
        if (!string.IsNullOrEmpty(countyCode))
        {
          CountryName = _dataContext.CountryDetails.FirstOrDefault(x => x.IsDeleted == false && x.Code == countyCode).Name;
        }
        return CountryName;
      }
      catch (ArgumentException)
      {
      }
      return null;
    }

    private async Task<List<OrganisationEligibleRole>> GetOrganisationEligibleRolesAsync(Organisation org, int supplierBuyerType)
    {
      var eligibleRoles = new List<OrganisationEligibleRole>();

      var defaultRoles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted).ToListAsync();
      var roles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted).ToListAsync();

      if (supplierBuyerType == 0) //Supplier
      {
        roles = roles.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Supplier || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToList();

        defaultRoles = defaultRoles.Where(ar => !ar.IsDeleted &&
          !roles.Any(r => r.Id == ar.Id) &&
          !string.IsNullOrEmpty(ar.DefaultEligibility) && ar.DefaultEligibility.StartsWith("1")
        ).ToList();

      }
      else if (supplierBuyerType == 1) //Buyer
      {
        roles = roles.Where(ar => !ar.IsDeleted &&
          ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
          ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
          (ar.TradeEligibility == RoleEligibleTradeType.Buyer || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToList();

        defaultRoles = defaultRoles.Where(ar => !ar.IsDeleted &&
          !roles.Any(r => r.Id == ar.Id) &&
          !string.IsNullOrEmpty(ar.DefaultEligibility) && ar.DefaultEligibility.Substring(1, 1) == "1"
        ).ToList();

      }
      else //Supplier & Buyer
      {
        roles = roles.Where(ar => !ar.IsDeleted &&
         ar.SubscriptionTypeEligibility == RoleEligibleSubscriptionType.Default &&
         ar.OrgTypeEligibility != RoleEligibleOrgType.Internal &&
         (ar.TradeEligibility == RoleEligibleTradeType.Supplier || ar.TradeEligibility == RoleEligibleTradeType.Buyer || ar.TradeEligibility == RoleEligibleTradeType.Both)
        ).ToList();

        defaultRoles = defaultRoles.Where(ar => !ar.IsDeleted &&
          !roles.Any(r => r.Id == ar.Id) &&
          !string.IsNullOrEmpty(ar.DefaultEligibility) && ar.DefaultEligibility.EndsWith("1")
        ).ToList();

      }

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

      defaultRoles.ForEach((defaultRole) =>
      {
        var eligibleRole = new OrganisationEligibleRole
        {
          CcsAccessRole = defaultRole,
          Organisation = org,
          MfaEnabled = defaultRole.MfaEnabled
        };
        eligibleRoles.Add(eligibleRole);
      });

      return eligibleRoles;
    }

    // #Auto validation
    #region auto validation

    public async Task<bool> ManualValidateOrganisation(string ciiOrganisationId, ManualValidateOrganisationStatus status)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }

      var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles)
                              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      User actionedBy = await _dataContext.User.Include(p => p.Party).ThenInclude(pe => pe.Person).FirstOrDefaultAsync(x => !x.IsDeleted && x.UserName == _requestContext.UserName && x.UserType == UserType.Primary);

      if (organisation.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier)
      {
        if (status == ManualValidateOrganisationStatus.Decline)
        {
          return await ManualValidateDecline(organisation, actionedBy);
        }
        else if (status == ManualValidateOrganisationStatus.Approve)
        {
          return await ManualValidateApprove(organisation, actionedBy);
        }
        //else if (status == ManualValidateOrganisationStatus.Remove)
        //{
        //  return await AutoValidateForInValidDomain(organisation, actionedBy, autoValidationDetails.CompanyHouseId, autoValidationDetails.IsFromBackgroundJob);
        //}
      }
      else
      {
        throw new InvalidOperationException();
      }

      return true;
    }

    public async Task<bool> AutoValidateOrganisationJob(string ciiOrganisationId)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }

      var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles)
                              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      //call lookup api
      bool isDomainValid = AutoValidateOrganisationDetails(ciiOrganisationId, "").Result.Item1;

      return isDomainValid;

      //TODO: Assing role to buyer or both type of org

    }

    // Registration
    public async Task<bool> AutoValidateOrganisationRegistration(string ciiOrganisationId, AutoValidationDetails autoValidationDetails)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }

      var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles)
                              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      // call lookup api
      bool isDomainValid = AutoValidateOrganisationDetails(ciiOrganisationId, autoValidationDetails.AdminEmailId).Result.Item1;
      User actionedBy = await _dataContext.User.Include(p => p.Party).ThenInclude(pe => pe.Person).FirstOrDefaultAsync(x => !x.IsDeleted && x.UserName == autoValidationDetails.AdminEmailId && x.UserType == UserType.Primary);

      // buyer and both only auto validated
      if ((organisation.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier))
      {
        // valid domain
        if (isDomainValid)
        {
          return await AutoValidateForValidDomain(organisation, actionedBy, autoValidationDetails.CompanyHouseId);
        }
        // invalid domain
        else
        {
          return await AutoValidateForInValidDomain(organisation, actionedBy, autoValidationDetails.CompanyHouseId, autoValidationDetails.IsFromBackgroundJob);
        }
      }
      else
      {
        //TODO: Add supplier roles
        await SupplierRoleAssignmentAsync(organisation, autoValidationDetails.AdminEmailId);
        return false;
      }
    }

    // Org eligiblity 
    public async Task UpdateOrgAutoValidationEligibleRolesAsync(string ciiOrganisationId, RoleEligibleTradeType newOrgType, List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete, string? companyHouseId)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }
      Guid groupId = Guid.NewGuid();
      List<OrganisationAuditEventInfo> auditEventLogs = new();
      OrganisationAuditInfo orgStatus = default;

      var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
                              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation != null)
      {
        int? oldOrgSupplierBuyerType = organisation.SupplierBuyerType;
        bool isOrgTypeSwitched = organisation.SupplierBuyerType != (int)newOrgType;
        organisation.RightToBuy = newOrgType != RoleEligibleTradeType.Supplier ? true : false;
        bool autoValidationSuccess = false;
        User actionedBy = await _dataContext.User.Include(p => p.Party).ThenInclude(pe => pe.Person).FirstOrDefaultAsync(x => !x.IsDeleted && x.UserName == _requestContext.UserName && x.UserType == UserType.Primary);

        if (newOrgType != RoleEligibleTradeType.Supplier)
        {
          var autoValidationOrgDetails = await AutoValidateOrganisationDetails(organisation.CiiOrganisationId);
          autoValidationSuccess = autoValidationOrgDetails != null ? autoValidationOrgDetails.Item1 : false;
        }

        // Switched from supplier to buyer or both
        if (isOrgTypeSwitched)
        {
          orgStatus = new OrganisationAuditInfo
          {
            Status = newOrgType == RoleEligibleTradeType.Supplier ? OrgAutoValidationStatus.ManualRemovalOfRightToBuy :
                                   (autoValidationSuccess ? OrgAutoValidationStatus.AutoApproved : OrgAutoValidationStatus.AutoPending),
            OrganisationId = organisation.Id,
            Actioned = OrganisationAuditActionType.Admin.ToString(),
            SchemeIdentifier = companyHouseId,
            ActionedBy = actionedBy?.UserName
          };
        }

        var rolesAssigned = await AddNewOrgRoles(rolesToAdd, rolesToDelete, organisation, newOrgType, autoValidationSuccess);
        var rolesUnassigned = await RemoveOrgRoles(rolesToDelete, organisation);

        // No event log if org was supplier and changes in role only.
        if (isOrgTypeSwitched)
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, GetOrgEventTypeChange((int)oldOrgSupplierBuyerType, (int)newOrgType), groupId, organisation.Id, companyHouseId, actionedBy: actionedBy));
        }
        if (!string.IsNullOrWhiteSpace(rolesAssigned) && (newOrgType != RoleEligibleTradeType.Supplier || (isOrgTypeSwitched && newOrgType == RoleEligibleTradeType.Supplier)))
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, companyHouseId, roles: rolesAssigned, actionedBy: actionedBy));
        }
        if (!string.IsNullOrWhiteSpace(rolesAssigned) && (newOrgType != RoleEligibleTradeType.Supplier || (isOrgTypeSwitched && newOrgType == RoleEligibleTradeType.Supplier)))
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleUnassigned, groupId, organisation.Id, companyHouseId, roles: rolesUnassigned, actionedBy: actionedBy));
        }

        // Org type change to supplier then notify all org admins
        if (isOrgTypeSwitched)
        {
          if (newOrgType == RoleEligibleTradeType.Supplier)
          {
            organisation.SupplierBuyerType = (int)RoleEligibleTradeType.Supplier;
            await NotifyAllOrgAdmins(organisation);
          }
          else
          {
            organisation.SupplierBuyerType = (int)newOrgType;
          }
        }

        await _dataContext.SaveChangesAsync();

        if (isOrgTypeSwitched && orgStatus != null)
        {
          await _organisationAuditService.CreateOrganisationAuditAsync(orgStatus);
        }

        await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);
        // Remove service client id inmemory cache since role update
        _localCacheService.Remove($"ORGANISATION_SERVICE_CLIENT_IDS-{ciiOrganisationId}");

      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    // Auto validate org details
    public async Task<Tuple<bool, string>> AutoValidateOrganisationDetails(string ciiOrganisationId, string adminEmailId = "")
    {
      if (string.IsNullOrWhiteSpace(adminEmailId))
      {
        var organisation = await _dataContext.Organisation.Include(er => er.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
                                .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

        if (organisation == null)
        {
          throw new ResourceNotFoundException();
        }

        var orgAdminAccessRoleId = organisation.OrganisationEligibleRoles.FirstOrDefault(x => !x.IsDeleted && x.CcsAccessRole?.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey).Id;

        var olderAdmin = await _dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
          .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
          .OrderBy(u => u.CreatedOnUtc)
          .FirstOrDefaultAsync(u => u.Party.Person.OrganisationId == organisation.Id && u.UserType == UserType.Primary &&
          u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId) && !u.IsDeleted);

        adminEmailId = olderAdmin?.UserName;
      }

      bool isDomainValid = false;
      if (!string.IsNullOrWhiteSpace(adminEmailId))
      {
        try
        {
          isDomainValid = await _lookUpService.IsDomainValidForAutoValidation(adminEmailId);
        }
        catch (Exception ex)
        {
          //TODO: lookup api not available logic
          Console.WriteLine(ex.Message);
        }
      }
      return Tuple.Create(isDomainValid, adminEmailId);
    }

    private async Task<bool> AutoValidateForValidDomain(Organisation organisation, User actionedBy, string schemeIdentifier)
    {
      Guid groupId = Guid.NewGuid();
      List<OrganisationAuditEventInfo> auditEventLogs = new();
      var adminUserDetails = _dataContext.User.Include(x => x.UserAccessRoles)
                            //.Include(x => x.Party).ThenInclude(x => x.Organisation)
                            .Where(x => !x.IsDeleted && x.UserName.ToLower() == actionedBy.UserName.ToLower() && x.UserType == UserType.Primary).FirstOrDefault();

      organisation.RightToBuy = true;

      // TODO: Auto validation Role assignment as per new logic buyer/both
      //  all role logs added owner Autovalidation
      //for org roles
      string rolesAsssignToOrg = await AutoValidationOrgRoleAssignmentAsync(organisation, isAutoValidationSuccess: true);
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToOrg, actionedBy: actionedBy));

      //for admin roles
      string rolesAsssignToAdmin = await AutoValidationAdminRoleAssignmentAsync(adminUserDetails, organisation.SupplierBuyerType, organisation.CiiOrganisationId, isAutoValidationSuccess: true);
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AdminRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToAdmin, actionedBy: actionedBy));
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AutomaticAcceptationRightToBuy, groupId, organisation.Id, schemeIdentifier, actionedBy: actionedBy));
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer ?
        OrganisationAuditEventType.OrganisationRegistrationTypeBuyer : OrganisationAuditEventType.OrganisationRegistrationTypeBoth,
        groupId, organisation.Id, schemeIdentifier));

      var orgStatus = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.AutoApproved,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Autovalidation.ToString(),
        SchemeIdentifier = schemeIdentifier,
        ActionedBy = actionedBy.UserName
      };
      // Events and log entry
      await _organisationAuditService.CreateOrganisationAuditAsync(orgStatus);
      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);

      try
      {
        await _dataContext.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }
      // Do entries in org audit table

      return true;
    }

    private async Task<bool> AutoValidateForInValidDomain(Organisation organisation, User actionedBy, string schemeIdentifier, bool isFromBackgroundJob = false)
    {
      Guid groupId = Guid.NewGuid();
      List<OrganisationAuditEventInfo> auditEventLogs = new();
      var adminUserDetails = _dataContext.User.Include(x => x.UserAccessRoles)
                            //.Include(x => x.Party).ThenInclude(x => x.Organisation)
                            .Where(x => !x.IsDeleted && x.UserName.ToLower() == actionedBy.UserName.ToLower() && x.UserType == UserType.Primary).FirstOrDefault();
      organisation.RightToBuy = false;

      //for org roles
      string rolesAsssignToOrg = await AutoValidationOrgRoleAssignmentAsync(organisation, isAutoValidationSuccess: false);
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToOrg, actionedBy: actionedBy));

      //for admin roles
      string rolesAsssignToAdmin = await AutoValidationAdminRoleAssignmentAsync(adminUserDetails, organisation.SupplierBuyerType, organisation.CiiOrganisationId, isAutoValidationSuccess: false);
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AdminRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToAdmin, actionedBy: actionedBy));

      // invalid
      // Send email to CCS admin to notify
      if (!isFromBackgroundJob)
      {
        await _ccsSsoEmailService.SendOrgPendingVerificationEmailToCCSAdminAsync(_applicationConfigurationInfo.OrgAutoValidation.CCSAdminEmailId, organisation.LegalName);
      }

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AutomaticDeclineRightToBuy, groupId, organisation.Id, schemeIdentifier, actionedBy: actionedBy));
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer ?
        OrganisationAuditEventType.OrganisationRegistrationTypeBuyer : OrganisationAuditEventType.OrganisationRegistrationTypeBoth,
        groupId, organisation.Id, schemeIdentifier, actionedBy: actionedBy));

      // TODO: AC23 change org type as buyer need to confirm
      organisation.SupplierBuyerType = (int)RoleEligibleTradeType.Buyer;

      try
      {
        await _dataContext.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }

      var orgStatus = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.AutoPending,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Autovalidation.ToString(),
        SchemeIdentifier = schemeIdentifier,
        ActionedBy = actionedBy.UserName
      };

      await _organisationAuditService.CreateOrganisationAuditAsync(orgStatus);
      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);
      return false;
    }

    private async Task<string> AutoValidationOrgRoleAssignmentAsync(Organisation organisation, bool isAutoValidationSuccess)
    {
      var defaultOrgRoles = await _dataContext.AutoValidationRole.Include(x => x.CcsAccessRole)
        .Where(ar => !ar.CcsAccessRole.IsDeleted && ar.AssignToOrg).ToListAsync();

      if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both)
      {
        defaultOrgRoles = isAutoValidationSuccess ? defaultOrgRoles.Where(o => o.IsBothSuccess == true).ToList() : defaultOrgRoles.Where(o => o.IsBothFailed == true).ToList();
      }
      else
      {
        defaultOrgRoles = isAutoValidationSuccess ? defaultOrgRoles.Where(o => o.IsBuyerSuccess == true).ToList() : defaultOrgRoles.Where(o => o.IsBuyerFailed == true).ToList();
      }

      StringBuilder rolesAssigned = new();
      foreach (var role in defaultOrgRoles.Select(x => x.CcsAccessRole))
      {
        if (!organisation.OrganisationEligibleRoles.Any(x => x.CcsAccessRoleId == role.Id))
        {
          var defaultOrgRole = new OrganisationEligibleRole
          {
            OrganisationId = organisation.Id,
            CcsAccessRoleId = role.Id
          };
          organisation.OrganisationEligibleRoles.Add(defaultOrgRole);
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRoleName : role.CcsAccessRoleName);
        }
      }
      await _dataContext.SaveChangesAsync();
      return rolesAssigned.ToString();
    }

    private async Task<string> AutoValidationAdminRoleAssignmentAsync(User adminDetails, int? orgType, string ciiOrganisation, bool isAutoValidationSuccess)
    {
      var defaultAdminRoles = await _dataContext.AutoValidationRole.Where(ar => ar.AssignToAdmin).ToListAsync();

      if (orgType == (int)RoleEligibleTradeType.Both)
      {
        defaultAdminRoles = isAutoValidationSuccess ? defaultAdminRoles.Where(o => o.IsBothSuccess == true).ToList() : defaultAdminRoles.Where(o => o.IsBothFailed == true).ToList();
      }
      else
      {
        defaultAdminRoles = isAutoValidationSuccess ? defaultAdminRoles.Where(o => o.IsBuyerSuccess == true).ToList() : defaultAdminRoles.Where(o => o.IsBuyerFailed == true).ToList();
      }

      var defaultRoles = await _dataContext.OrganisationEligibleRole
            .Where(r => r.Organisation.CiiOrganisationId == ciiOrganisation &&
            defaultAdminRoles.Select(x => x.CcsAccessRoleId).Contains(r.CcsAccessRoleId))
            .ToListAsync();

      // if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      StringBuilder rolesAssigned = new();
      foreach (var role in defaultRoles)
      {
        // additional roles for admin user added if not exist
        if (!adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == role.Id))
        {
          var defaultUserRole = new UserAccessRole
          {
            OrganisationEligibleRoleId = role.Id
          };
          adminDetails.UserAccessRoles.Add(defaultUserRole);
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRole.CcsAccessRoleName : role.CcsAccessRole.CcsAccessRoleName);
        }
      }
      return rolesAssigned.ToString();
    }

    private async Task SupplierRoleAssignmentAsync(Organisation organisation, string adminEmailId)
    {
      var adminDetails = _dataContext.User.Include(x => x.UserAccessRoles)
                            //.Include(x => x.Party).ThenInclude(x => x.Organisation)
                            .Where(x => !x.IsDeleted && x.UserName.ToLower() == adminEmailId.ToLower() && x.UserType == UserType.Primary).FirstOrDefault();


      var defaultOrgRoles = await _dataContext.AutoValidationRole.Include(x => x.CcsAccessRole)
        .Where(ar => !ar.CcsAccessRole.IsDeleted && ar.AssignToOrg && ar.IsSupplier).ToListAsync();


      StringBuilder rolesAssigned = new();
      foreach (var role in defaultOrgRoles.Select(x => x.CcsAccessRole))
      {
        if (!organisation.OrganisationEligibleRoles.Any(x => x.CcsAccessRoleId == role.Id))
        {
          var defaultOrgRole = new OrganisationEligibleRole
          {
            OrganisationId = organisation.Id,
            CcsAccessRoleId = role.Id
          };
          organisation.OrganisationEligibleRoles.Add(defaultOrgRole);
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRoleName : role.CcsAccessRoleName);
        }
      }
      await _dataContext.SaveChangesAsync();

      // Admin role assignment

      var defaultAdminRoles = await _dataContext.AutoValidationRole.Where(ar => ar.AssignToAdmin && ar.IsSupplier).ToListAsync();

      var defaultRoles = await _dataContext.OrganisationEligibleRole.Where(r => r.Organisation.CiiOrganisationId == organisation.CiiOrganisationId &&
                          defaultAdminRoles.Select(x => x.CcsAccessRoleId).Contains(r.CcsAccessRoleId)).ToListAsync();

      foreach (var role in defaultRoles)
      {
        // additional roles for admin user added if not exist
        if (!adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == role.Id))
        {
          var defaultUserRole = new UserAccessRole
          {
            OrganisationEligibleRoleId = role.Id
          };
          adminDetails.UserAccessRoles.Add(defaultUserRole);
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRole.CcsAccessRoleName : role.CcsAccessRole.CcsAccessRoleName);
        }
      }

      await _dataContext.SaveChangesAsync();
    }

    private OrganisationAuditEventInfo CreateAutoValidationEventLog(OrganisationAuditActionType actioned, OrganisationAuditEventType eventType, Guid groupId, int orgId, string schemeIdentifier, string roles = "", User actionedBy = null)
    {
      return new OrganisationAuditEventInfo
      {
        Actioned = actioned.ToString(),
        Event = eventType.ToString(),
        GroupId = groupId,
        OrganisationId = orgId,
        Roles = roles,
        SchemeIdentifier = schemeIdentifier,
        FirstName = actionedBy?.Party?.Person?.FirstName,
        LastName = actionedBy?.Party?.Person?.LastName,
        ActionedBy = actionedBy?.UserName
      };
    }

    // Service elegiblity 
    private async Task<string> AddNewOrgRoles(List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete, Organisation organisation, RoleEligibleTradeType newOrgType, bool autoValidationPassed = false)
    {
      var ccsAccessRoles = await _dataContext.CcsAccessRole.ToListAsync();
      StringBuilder rolesAssigned = new();

      // list of roles to remove for non verified buyer
      List<AutoValidationRole> autoValidationFailedRolesForOrg = new();

      if (newOrgType == RoleEligibleTradeType.Supplier)
      {
        autoValidationFailedRolesForOrg = await _dataContext.AutoValidationRole.Where(x => x.AssignToOrg == true && !x.IsSupplier).ToListAsync();
      }
      else if (newOrgType == RoleEligibleTradeType.Buyer)
      {
        autoValidationFailedRolesForOrg = await _dataContext.AutoValidationRole.Where(x => x.AssignToOrg == true && (autoValidationPassed ? !x.IsBuyerSuccess : !x.IsBuyerFailed)).ToListAsync();
      }
      else if (newOrgType == RoleEligibleTradeType.Both)
      {
        autoValidationFailedRolesForOrg = await _dataContext.AutoValidationRole.Where(x => x.AssignToOrg == true && (autoValidationPassed ? !x.IsBothSuccess : !x.IsBothFailed)).ToListAsync();
      }

      var roleToAddWithOutRemovingVerfiedBuyer = rolesToAdd.ToList();

      // Remove roles not valid for new org type
      foreach (var role in autoValidationFailedRolesForOrg)
      {
        var rolesFoundToRemove = rolesToAdd.Where(r => r.RoleId == role.CcsAccessRoleId).ToList();
        var isRoleExistInDeleteList = rolesToDelete.Any(r => r.RoleId == role.CcsAccessRoleId);
        var existingRolesToRemove = organisation.OrganisationEligibleRoles.Where(r => r.CcsAccessRoleId == role.CcsAccessRoleId && !r.IsDeleted).ToList();
        foreach (var rolesRemove in rolesFoundToRemove)
        {
          rolesToAdd.Remove(rolesRemove);
        }
        // remove existing roles when type changed
        foreach (var roleRemove in existingRolesToRemove)
        {
          //organisation.OrganisationEligibleRoles.Remove(roleRemove);
          if (!isRoleExistInDeleteList)
          {
            rolesToDelete.Add(new OrganisationRole
            {
              RoleId = role.CcsAccessRoleId,
            });
          }
        }
      }

      if (rolesToAdd != null && rolesToAdd.Any())
      {
        if (!rolesToAdd.All(ar => ccsAccessRoles.Any(r => r.Id == ar.RoleId)))
        {
          throw new CcsSsoException("INVALID_ROLES_TO_ADD");
        }

        List<OrganisationEligibleRole> addedEligibleRoles = new List<OrganisationEligibleRole>();
        rolesToAdd.ForEach((addedRole) =>
        {
          if (!organisation.OrganisationEligibleRoles.Any(oer => !oer.IsDeleted && oer.CcsAccessRoleId == addedRole.RoleId))
          {
            addedEligibleRoles.Add(new OrganisationEligibleRole
            {
              OrganisationId = organisation.Id,
              CcsAccessRoleId = addedRole.RoleId
            });
            rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + addedRole.RoleName : addedRole.RoleName);
          }
        });
        _dataContext.OrganisationEligibleRole.AddRange(addedEligibleRoles);
      }

      return rolesAssigned.ToString();
    }

    private async Task<string> RemoveOrgRoles(List<OrganisationRole> rolesToDelete, Organisation organisation)
    {
      var userAccessRolesForOrgUsers = await _dataContext.UserAccessRole.Where(uar => !uar.IsDeleted &&
                                         uar.OrganisationEligibleRole.OrganisationId == organisation.Id).ToListAsync();
      StringBuilder rolesAssigned = new();

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

        var userAccessRolesWithDeletedRoles = userAccessRolesForOrgUsers
          .Where(uar => deletingRoleIds.Contains(uar.OrganisationEligibleRole.CcsAccessRoleId)).ToList();

        deletingOrgEligibleRoles.ForEach((deletingOrgEligibleRole) =>
        {
          deletingOrgEligibleRole.IsDeleted = true;
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + deletingOrgEligibleRole.CcsAccessRole.CcsAccessRoleName : deletingOrgEligibleRole.CcsAccessRole.CcsAccessRoleName);
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

      return rolesAssigned.ToString();
    }

    private static OrganisationAuditEventType GetOrgEventTypeChange(int oldOrgSupplierBuyerType, int newOrgSupplierBuyerType)
    {
      if (oldOrgSupplierBuyerType == (int)RoleEligibleTradeType.Supplier)
      {
        return newOrgSupplierBuyerType == (int)RoleEligibleTradeType.Buyer ? OrganisationAuditEventType.OrganisationTypeSupplierToBuyer : OrganisationAuditEventType.OrganisationTypeSupplierToBoth;
      }
      else if (oldOrgSupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      {
        return newOrgSupplierBuyerType == (int)RoleEligibleTradeType.Supplier ? OrganisationAuditEventType.OrganisationTypeBuyerToSupplier : OrganisationAuditEventType.OrganisationTypeBuyerToBoth;
      }
      else
      {
        return newOrgSupplierBuyerType == (int)RoleEligibleTradeType.Supplier ? OrganisationAuditEventType.OrganisationTypeBothToSupplier : OrganisationAuditEventType.OrganisationTypeBothToBuyer;
      }
    }

    private async Task NotifyAllOrgAdmins(Organisation organisation)
    {
      List<User> allActiveAdminsOfOrg = await GetAdminUsers(organisation);

      foreach (var admin in allActiveAdminsOfOrg)
      {
        // email all admins to notify org type change to supplier and right to buy is removed
        await _ccsSsoEmailService.SendOrgBuyerStatusChangeUpdateToAllAdminsAsync(admin.UserName);
      }
    }

    private async Task<List<User>> GetAdminUsers(Organisation organisation)
    {
      // TODO: find better way to get all admin
      // get all admins
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
      .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisation.Id && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;

      var allActiveAdminsOfOrg = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
        .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Where(u => u.Party.Person.Organisation.CiiOrganisationId == organisation.CiiOrganisationId && u.UserType == UserType.Primary &&
        u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId) && u.AccountVerified && !u.IsDeleted)
        .ToListAsync();
      return allActiveAdminsOfOrg;
    }

    private async Task<bool> ManualValidateDecline(Organisation organisation, User actionedBy)
    {
      Guid groupId = Guid.NewGuid();

      List<OrganisationAuditEventInfo> auditEventLogs = new();

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.ManualDeclineRightToBuy, groupId, organisation.Id, "", null, actionedBy: actionedBy));

      if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrganisationTypeBuyerToSupplier, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      }
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both)
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrganisationTypeBothToSupplier, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      }

      organisation.RightToBuy = false;
      organisation.SupplierBuyerType = (int)RoleEligibleTradeType.Supplier;

      var organisationAuditInfo = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.ManuallyDecliend,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Admin.ToString(),
        ActionedBy = actionedBy.UserName
      };

      await _organisationAuditService.UpdateOrganisationAuditAsync(organisationAuditInfo);

      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);

      try
      {
        await _dataContext.SaveChangesAsync();

        List<User> allActiveAdminsOfOrg = await GetAdminUsers(organisation);

        foreach (var admin in allActiveAdminsOfOrg)
        {
          //TODO: Send email
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }

      return true;
    }

    private async Task<bool> ManualValidateApprove(Organisation organisation, User actionedBy)
    {
      Guid groupId = Guid.NewGuid();

      List<OrganisationAuditEventInfo> auditEventLogs = new();

      List<User> allActiveAdminsOfOrg = await GetAdminUsers(organisation);

      string rolesAsssignToOrg = await ManualValidateOrgRoleAssignmentAsync(organisation);
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, "", rolesAsssignToOrg, actionedBy: actionedBy));
      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.ManualAcceptationRightToBuy, groupId, organisation.Id, "", null, actionedBy: actionedBy));

      await ManualValidateAdminRoleAssignmentAsync(organisation, allActiveAdminsOfOrg);

      organisation.RightToBuy = true;

      var organisationAuditInfo = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.ManuallyApproved,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Admin.ToString(),
        ActionedBy = actionedBy.UserName
      };

      await _organisationAuditService.UpdateOrganisationAuditAsync(organisationAuditInfo);

      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);

      try
      {
        await _dataContext.SaveChangesAsync();

        foreach (var admin in allActiveAdminsOfOrg)
        {
          //TODO: Send email
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }

      return true;
    }

    private async Task<string> ManualValidateOrgRoleAssignmentAsync(Organisation organisation)
    {
      var defaultOrgRoles = await _dataContext.AutoValidationRole.Include(x => x.CcsAccessRole)
        .Where(ar => !ar.CcsAccessRole.IsDeleted && ar.AssignToOrg).ToListAsync();

      if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      {
        defaultOrgRoles = defaultOrgRoles.Where(o => o.IsBuyerSuccess == true).ToList();
      }
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both)
      {
        defaultOrgRoles = defaultOrgRoles.Where(o => o.IsBothSuccess == true).ToList();
      }

      StringBuilder rolesAssigned = new();
      foreach (var role in defaultOrgRoles.Select(x => x.CcsAccessRole))
      {
        if (!organisation.OrganisationEligibleRoles.Any(x => x.CcsAccessRoleId == role.Id))
        {
          var defaultOrgRole = new OrganisationEligibleRole
          {
            OrganisationId = organisation.Id,
            CcsAccessRoleId = role.Id
          };
          organisation.OrganisationEligibleRoles.Add(defaultOrgRole);
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRoleName : role.CcsAccessRoleName);
        }
      }

      await _dataContext.SaveChangesAsync();

      return rolesAssigned.ToString();
    }

    private async Task ManualValidateAdminRoleAssignmentAsync(Organisation organisation, List<User> allActiveAdminsOfOrg)
    {
      var defaultAdminRoles = await _dataContext.AutoValidationRole.Where(ar => ar.AssignToAdmin).ToListAsync();

      if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      {
        defaultAdminRoles = defaultAdminRoles.Where(o => o.IsBuyerSuccess == true).ToList();
      }
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both)
      {
        defaultAdminRoles = defaultAdminRoles.Where(o => o.IsBothSuccess == true).ToList();
      }

      var defaultRoles = await _dataContext.OrganisationEligibleRole
            .Where(r => r.Organisation.CiiOrganisationId == organisation.CiiOrganisationId &&
            defaultAdminRoles.Select(x => x.CcsAccessRoleId).Contains(r.CcsAccessRoleId))
            .ToListAsync();

      foreach (var role in defaultRoles)
      {
        foreach (var adminDetails in allActiveAdminsOfOrg)
        {
          if (!adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == role.Id))
          {
            var defaultUserRole = new UserAccessRole
            {
              OrganisationEligibleRoleId = role.Id
            };
            adminDetails.UserAccessRoles.Add(defaultUserRole);
          }
        }
      }

      await _dataContext.SaveChangesAsync();
    }

    #endregion

  }
}
