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
using System.Data;
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
    private readonly IUserProfileRoleApprovalService _userProfileRoleApprovalService;
    private readonly IServiceRoleGroupMapperService _rolesToServiceRoleGroupMapperService;

    public OrganisationProfileService(IDataContext dataContext, IContactsHelperService contactsHelper, ICcsSsoEmailService ccsSsoEmailService,
      ICiiService ciiService, IAdaptorNotificationService adapterNotificationService,
      IWrapperCacheService wrapperCacheService, ILocalCacheService localCacheService,
      ApplicationConfigurationInfo applicationConfigurationInfo, RequestContext requestContext, IIdamService idamService, IRemoteCacheService remoteCacheService,
      ILookUpService lookUpService, IOrganisationAuditService organisationAuditService, IOrganisationAuditEventService organisationAuditEventService,
      IUserProfileRoleApprovalService userProfileRoleApprovalService, IServiceRoleGroupMapperService rolesToRoleServiceMapperService)
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
      _userProfileRoleApprovalService = userProfileRoleApprovalService;
      _rolesToServiceRoleGroupMapperService = rolesToRoleServiceMapperService;
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
        OrganisationUri = organisationProfileInfo.Identifier.Uri?.Trim(),
        DomainName = organisationProfileInfo.Detail.DomainName
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
            CreationDate = organisation.CreatedOnUtc.ToString(DateTimeFormat.DateFormat),
            DomainName = organisation.DomainName
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

    public async Task ManualValidateOrganisation(string ciiOrganisationId, ManualValidateOrganisationStatus status)
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
          await ManualValidateDecline(organisation, actionedBy);
        }
        else if (status == ManualValidateOrganisationStatus.Approve)
        {
          await ManualValidateApprove(organisation, actionedBy);
        }
        else if (status == ManualValidateOrganisationStatus.Remove)
        {
          await ManualValidateRemove(organisation, actionedBy);
        }
      }
      else
      {
        throw new InvalidOperationException();
      }
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
    public async Task UpdateOrgAutoValidationEligibleRolesAsync(string ciiOrganisationId, RoleEligibleTradeType newOrgType, List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete, List<OrganisationRole> rolesToAutoValid, string? companyHouseId)
    {
      if (!_applicationConfigurationInfo.OrgAutoValidation.Enable)
      {
        throw new InvalidOperationException();
      }
      if (!Enum.IsDefined(typeof(RoleEligibleTradeType), newOrgType))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
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
        bool autoValidationSuccess = false;
        bool alreadyVerifiedBuyer = false;
        User actionedBy = await _dataContext.User.Include(p => p.Party).ThenInclude(pe => pe.Person).FirstOrDefaultAsync(x => !x.IsDeleted && x.UserName == _requestContext.UserName && x.UserType == UserType.Primary);

        if (isOrgTypeSwitched && organisation.RightToBuy != true && newOrgType != RoleEligibleTradeType.Supplier)
        {
          var autoValidationOrgDetails = await AutoValidateOrganisationDetails(organisation.CiiOrganisationId);
          autoValidationSuccess = autoValidationOrgDetails != null ? autoValidationOrgDetails.Item1 : false;
          organisation.RightToBuy = autoValidationSuccess;
        }
        else
        {
          organisation.RightToBuy = newOrgType != RoleEligibleTradeType.Supplier ? organisation.RightToBuy : false;
          alreadyVerifiedBuyer = organisation.RightToBuy ?? false;
          autoValidationSuccess = organisation.RightToBuy ?? false;
        }

        // Switched from supplier to buyer or both
        if (isOrgTypeSwitched)
        {
          orgStatus = new OrganisationAuditInfo
          {
            Status = newOrgType == RoleEligibleTradeType.Supplier ? OrgAutoValidationStatus.ManualRemovalOfRightToBuy :
                                   (autoValidationSuccess ? OrgAutoValidationStatus.AutoApproved : OrgAutoValidationStatus.ManualPending),
            OrganisationId = organisation.Id,
            Actioned = OrganisationAuditActionType.Admin.ToString(),
            SchemeIdentifier = companyHouseId,
            ActionedBy = actionedBy?.UserName
          };
        }

        var rolesAssigned = await AddNewOrgRoles(rolesToAdd, rolesToDelete, rolesToAutoValid, organisation, newOrgType, autoValidationSuccess);
        var rolesUnassigned = await RemoveOrgRoles(rolesToDelete, organisation);
        await AdminRoleAssignment(organisation, newOrgType, autoValidationSuccess);

        // No event log if org was supplier and changes in role only.
        if (isOrgTypeSwitched)
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, GetOrgEventTypeChange((int)oldOrgSupplierBuyerType, (int)newOrgType), groupId, organisation.Id, companyHouseId, actionedBy: actionedBy));
        }

        if (isOrgTypeSwitched && !alreadyVerifiedBuyer && newOrgType != RoleEligibleTradeType.Supplier)
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, autoValidationSuccess ? OrganisationAuditEventType.AutomaticAcceptationRightToBuy : OrganisationAuditEventType.NotRecognizedAsVerifiedBuyer, groupId, organisation.Id, companyHouseId));
        }

        // Verified buyer only (Auto validated) roles only assigned
        if (!string.IsNullOrWhiteSpace(rolesAssigned.Item1) && (newOrgType != RoleEligibleTradeType.Supplier || (isOrgTypeSwitched && newOrgType == RoleEligibleTradeType.Supplier)))
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, companyHouseId, roles: rolesAssigned.Item1));
        }
        // Normal roles assigned
        if (!string.IsNullOrWhiteSpace(rolesAssigned.Item2) && (newOrgType != RoleEligibleTradeType.Supplier || (isOrgTypeSwitched && newOrgType == RoleEligibleTradeType.Supplier)))
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, companyHouseId, roles: rolesAssigned.Item2, actionedBy: actionedBy));
        }
        if (!string.IsNullOrWhiteSpace(rolesUnassigned) && (newOrgType != RoleEligibleTradeType.Supplier || (isOrgTypeSwitched && newOrgType == RoleEligibleTradeType.Supplier)))
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
          await _organisationAuditService.UpdateOrganisationAuditAsync(orgStatus);
        }

        await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);
        // Remove service client id inmemory cache since role update
        _localCacheService.Remove($"ORGANISATION_SERVICE_CLIENT_IDS-{ciiOrganisationId}");

      }
      else
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
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


    private async Task<bool> AutoValidateForValidDomain(Organisation organisation, User actionedBy, string schemeIdentifier, bool isFromBackgroundJob = false)
    {
      Guid groupId = Guid.NewGuid();
      List<OrganisationAuditEventInfo> auditEventLogs = new();
      var adminUserDetails = _dataContext.User.Include(x => x.UserAccessRoles)
                            //.Include(x => x.Party).ThenInclude(x => x.Organisation)
                            .Where(x => !x.IsDeleted && x.UserName.ToLower() == actionedBy.UserName.ToLower() && x.UserType == UserType.Primary).FirstOrDefault();

      organisation.RightToBuy = true;


      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation,
        organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer ? OrganisationAuditEventType.OrganisationRegistrationTypeBuyer : OrganisationAuditEventType.OrganisationRegistrationTypeBoth,
        groupId, organisation.Id, schemeIdentifier, actionedBy: actionedBy));

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AutomaticAcceptationRightToBuy, groupId, organisation.Id, schemeIdentifier));

      // TODO: Auto validation Role assignment as per new logic buyer/both
      //  all role logs added owner Autovalidation
      //for org roles
      string rolesAsssignToOrg = await AutoValidationOrgRoleAssignmentAsync(organisation, isAutoValidationSuccess: true);
      if (!string.IsNullOrWhiteSpace(rolesAsssignToOrg))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToOrg));
      }
      //for admin roles
      if (isFromBackgroundJob)
      {
        await AutoValidationAdminRolesForBackgroundJob(organisation, schemeIdentifier, groupId, auditEventLogs, adminUserDetails,true);
      }
      else
      {
        string rolesAsssignToAdmin = await AutoValidationAdminRoleAssignmentAsync(adminUserDetails, organisation.SupplierBuyerType, organisation.CiiOrganisationId, isAutoValidationSuccess: true);
        if (!string.IsNullOrWhiteSpace(rolesAsssignToAdmin))
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AdminRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToAdmin));
        }
      }

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

    private async Task AutoValidationAdminRolesForBackgroundJob(Organisation organisation, string schemeIdentifier, Guid groupId, List<OrganisationAuditEventInfo> auditEventLogs, User adminUserDetails, bool isAutoValidationSuccess)
    {
      // assign the oldest admin auto validation roles without role approval validation check
      string rolesAsssignToAdmin = await AutoValidationAdminRoleAssignmentAsync(adminUserDetails, organisation.SupplierBuyerType, organisation.CiiOrganisationId, isAutoValidationSuccess: isAutoValidationSuccess, false);
      if (!string.IsNullOrWhiteSpace(rolesAsssignToAdmin))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AdminRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToAdmin));
      }

      // assign auto validation roles to all other admins with role approval validation check
      List<User> allAdminsOfOrg = await GetAdminUsers(organisation, false);
      allAdminsOfOrg.Remove(allAdminsOfOrg.FirstOrDefault(x => x.Id == adminUserDetails.Id));

      foreach (var otherAdminUser in allAdminsOfOrg)
      {
        await AutoValidationAdminRoleAssignmentAsync(otherAdminUser, organisation.SupplierBuyerType, organisation.CiiOrganisationId, isAutoValidationSuccess: isAutoValidationSuccess, true);
      }
    }

    private async Task<bool> AutoValidateForInValidDomain(Organisation organisation, User actionedBy, string schemeIdentifier, bool isFromBackgroundJob = false)
    {
      Guid groupId = Guid.NewGuid();
      List<OrganisationAuditEventInfo> auditEventLogs = new();
      var adminUserDetails = _dataContext.User.Include(x => x.UserAccessRoles)
                            //.Include(x => x.Party).ThenInclude(x => x.Organisation)
                            .Where(x => !x.IsDeleted && x.UserName.ToLower() == actionedBy.UserName.ToLower() && x.UserType == UserType.Primary).FirstOrDefault();
      organisation.RightToBuy = false;

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation,
        organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer ? OrganisationAuditEventType.OrganisationRegistrationTypeBuyer : OrganisationAuditEventType.OrganisationRegistrationTypeBoth,
        groupId, organisation.Id, schemeIdentifier, actionedBy: actionedBy));

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.NotRecognizedAsVerifiedBuyer, groupId, organisation.Id, schemeIdentifier));

      //for org roles
      string rolesAsssignToOrg = await AutoValidationOrgRoleAssignmentAsync(organisation, isAutoValidationSuccess: false);
      if (!string.IsNullOrWhiteSpace(rolesAsssignToOrg))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToOrg));
      }

      //for admin roles
      if (isFromBackgroundJob)
      {
        await AutoValidationAdminRolesForBackgroundJob(organisation, schemeIdentifier, groupId, auditEventLogs, adminUserDetails,false);
      }
      else
      {
        string rolesAsssignToAdmin = await AutoValidationAdminRoleAssignmentAsync(adminUserDetails, organisation.SupplierBuyerType, organisation.CiiOrganisationId, isAutoValidationSuccess: false);

        if (!string.IsNullOrWhiteSpace(rolesAsssignToAdmin))
        {
          auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Autovalidation, OrganisationAuditEventType.AdminRoleAssigned, groupId, organisation.Id, schemeIdentifier, rolesAsssignToAdmin));
        }
      }

      // invalid
      // Send email to CCS admin to notify
      if (!isFromBackgroundJob)
      {
        foreach (var email in _applicationConfigurationInfo.OrgAutoValidation.CCSAdminEmailIds)
        {
          await _ccsSsoEmailService.SendOrgPendingVerificationEmailToCCSAdminAsync(email, organisation.LegalName);
        }
      }

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

    // Registration auto validation role assignment
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
        if (!organisation.OrganisationEligibleRoles.Any(x => x.CcsAccessRoleId == role.Id && !x.IsDeleted))
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
      return await AutoValidationAdminRoleAssignmentAsync(adminDetails, orgType, ciiOrganisation, isAutoValidationSuccess, false);
    }
    private async Task<string> AutoValidationAdminRoleAssignmentAsync(User adminDetails, int? orgType, string ciiOrganisation, bool isAutoValidationSuccess, bool roleApprovalCheckRequired = false)
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

      var roleIds = defaultAdminRoles.Select(x => x.CcsAccessRoleId);

      if (isAutoValidationSuccess)
      {
        var successAdminRoleKeys = orgType == (int)RoleEligibleTradeType.Both ? _applicationConfigurationInfo.OrgAutoValidation.BothSuccessAdminRoles : _applicationConfigurationInfo.OrgAutoValidation.BuyerSuccessAdminRoles;
        var successAdminRoleIds = await _dataContext.CcsAccessRole.Where(x => successAdminRoleKeys.Contains(x.CcsAccessRoleNameKey)).Select(r => r.Id).ToListAsync();
        roleIds = roleIds.Union(successAdminRoleIds);
      }

      var defaultRoles = await _dataContext.OrganisationEligibleRole 
            .Where(r => r.Organisation.CiiOrganisationId == ciiOrganisation && !r.IsDeleted &&
            roleIds.Contains(r.CcsAccessRoleId))
            .ToListAsync();

      var organisation = await _dataContext.Organisation.Where(o => o.CiiOrganisationId == ciiOrganisation && !o.IsDeleted).FirstOrDefaultAsync();


      // if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      StringBuilder rolesAssigned = new();
      foreach (var role in defaultRoles)
      {
        // additional roles for admin user added if not exist
        if (adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == role.Id && !x.IsDeleted))
        {
          continue;
        }

        if (!roleApprovalCheckRequired)
        {
          var defaultUserRole = new UserAccessRole
          {
            OrganisationEligibleRoleId = role.Id
          };
          adminDetails.UserAccessRoles.Add(defaultUserRole);
          rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRole.CcsAccessRoleName : role.CcsAccessRole.CcsAccessRoleName);
        }
        else
        {
          await AddRoleWithRoleApprovalCheck(adminDetails, organisation, rolesAssigned, role);
        }
      }
      return rolesAssigned.ToString();
    }

    private async Task AddRoleWithRoleApprovalCheck(User adminDetails, Organisation organisation, StringBuilder rolesAssigned, OrganisationEligibleRole role)
    {
      var IsRoleValid = RoleApprovalRequiredCheck(organisation, role.CcsAccessRole.ApprovalRequired, adminDetails);

      if (IsRoleValid)
      {
        var defaultUserRole = new UserAccessRole
        {
          OrganisationEligibleRoleId = role.Id
        };
        adminDetails.UserAccessRoles.Add(defaultUserRole);
        rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + role.CcsAccessRole.CcsAccessRoleName : role.CcsAccessRole.CcsAccessRoleName);
      }
      else
      {
        UserProfileEditRequestInfo userProfileRequestInfo = new UserProfileEditRequestInfo
        {
          UserName = adminDetails.UserName,
          OrganisationId = organisation.CiiOrganisationId,
          Detail = new UserRequestDetail
          {
            RoleIds = new List<int> { role.Id }
          }
        };
        await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(userProfileRequestInfo, sendEmailNotification: false);
      }
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
        if (!organisation.OrganisationEligibleRoles.Any(x => x.CcsAccessRoleId == role.Id && !x.IsDeleted))
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
                          !r.IsDeleted && defaultAdminRoles.Select(x => x.CcsAccessRoleId).Contains(r.CcsAccessRoleId)).ToListAsync();

      foreach (var role in defaultRoles)
      {
        // additional roles for admin user added if not exist
        if (!adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == role.Id && !x.IsDeleted))
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
    private async Task<Tuple<string, string>> AddNewOrgRoles(List<OrganisationRole> rolesToAdd, List<OrganisationRole> rolesToDelete, List<OrganisationRole> rolesToAutoValid, Organisation organisation, RoleEligibleTradeType newOrgType, bool autoValidationPassed = false)
    {
      var ccsAccessRoles = await _dataContext.CcsAccessRole.ToListAsync();
      StringBuilder verifiedBuyerOnlyRolesAssigned = new();
      StringBuilder rolesAssigned = new();

      // list of roles to remove for non verified buyer
      List<AutoValidationRole> autoValidationFailedRolesForOrg = new();
      var autoValidationRoles = await _dataContext.AutoValidationRole.ToListAsync();
      var verifiedBuyerOnlyRolesForOrg = autoValidationRoles.Where(x => x.IsBuyerSuccess && x.AssignToOrg).ToList();
      var autoValidationRoleIds = new List<int>();

      if (newOrgType == RoleEligibleTradeType.Supplier)
      {
        autoValidationFailedRolesForOrg = autoValidationRoles.Where(x => !x.IsSupplier).ToList();
      }
      else if (newOrgType == RoleEligibleTradeType.Buyer)
      {
        autoValidationFailedRolesForOrg = autoValidationRoles.Where(x => autoValidationPassed ? !x.IsBuyerSuccess : !x.IsBuyerFailed).ToList();
        autoValidationRoleIds = autoValidationRoles.Where(x => x.IsBuyerSuccess).Select(x => x.CcsAccessRoleId).ToList();
      }
      else if (newOrgType == RoleEligibleTradeType.Both)
      {
        autoValidationFailedRolesForOrg = autoValidationRoles.Where(x => autoValidationPassed ? !x.IsBothSuccess : !x.IsBothFailed).ToList();
        autoValidationRoleIds = autoValidationRoles.Where(x => x.IsBothSuccess).Select(x => x.CcsAccessRoleId).ToList();
      }

      if (!autoValidationPassed && rolesToAutoValid != null && rolesToAutoValid.Count > 0)
      {
        var rolesRequiredManualVerification = rolesToAutoValid.Where(r => autoValidationRoleIds.Contains(r.RoleId)).ToList();
        if (rolesRequiredManualVerification != null && rolesRequiredManualVerification.Count > 0)
        {
          List<OrganisationEligibleRolePending> addedEligibleRolesPending = new List<OrganisationEligibleRolePending>();

          var organisationEligibleRolePending = await _dataContext.OrganisationEligibleRolePending.Where(x => x.OrganisationId == organisation.Id).ToListAsync();

          rolesRequiredManualVerification.ForEach((addedRole) =>
          {
            if (!organisationEligibleRolePending.Any(x => !x.IsDeleted && x.CcsAccessRoleId == addedRole.RoleId))
            {
              addedEligibleRolesPending.Add(new OrganisationEligibleRolePending
              {
                OrganisationId = organisation.Id,
                CcsAccessRoleId = addedRole.RoleId
              });
            }
          });

          _dataContext.OrganisationEligibleRolePending.AddRange(addedEligibleRolesPending);
        }
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

            // two type of logs one for auto validation roles like Fleet role etc. and one seperate for normal roles like RMI etc.
            if (verifiedBuyerOnlyRolesForOrg.Any(x => x.CcsAccessRoleId == addedRole.RoleId))
            {
              verifiedBuyerOnlyRolesAssigned.Append(verifiedBuyerOnlyRolesAssigned.Length > 0 ? "," + addedRole.RoleName : addedRole.RoleName);
            }
            else
            {
              rolesAssigned.Append(rolesAssigned.Length > 0 ? "," + addedRole.RoleName : addedRole.RoleName);
            }
          }
        });
        _dataContext.OrganisationEligibleRole.AddRange(addedEligibleRoles);
      }
      await _dataContext.SaveChangesAsync();

      return Tuple.Create(verifiedBuyerOnlyRolesAssigned.ToString(), rolesAssigned.ToString());
    }

    private async Task<string> RemoveOrgRoles(List<OrganisationRole> rolesToDelete, Organisation organisation)
    {
      StringBuilder rolesRemoved = new();

      if (rolesToDelete != null && rolesToDelete.Any())
      {
        var deletingRoleIds = rolesToDelete.Select(r => r.RoleId).ToList();

        var userAccessRolesForOrgUsers = await _dataContext.UserAccessRole.Include(gr => gr.OrganisationEligibleRole).Where(uar => !uar.IsDeleted &&
                                           uar.OrganisationEligibleRole.OrganisationId == organisation.Id).ToListAsync();

        var deletingOrgEligibleRoles = organisation.OrganisationEligibleRoles.Where(oer => deletingRoleIds.Contains(oer.CcsAccessRoleId) && !oer.IsDeleted).ToList();

        var orgGroupRolesWithDeletedRoles = await _dataContext.OrganisationGroupEligibleRole
          .Where(oger => !oger.IsDeleted && oger.OrganisationEligibleRole.OrganisationId == organisation.Id && deletingRoleIds.Contains(oger.OrganisationEligibleRole.CcsAccessRoleId))
          .ToListAsync();

        var userAccessRolesWithDeletedRoles = userAccessRolesForOrgUsers
          .Where(uar => deletingRoleIds.Contains(uar.OrganisationEligibleRole.CcsAccessRoleId)).ToList();

        var allAccessRolePending = await _dataContext.UserAccessRolePending.Where(u => !u.IsDeleted && u.Status == (int)UserPendingRoleStaus.Pending).ToListAsync();
        deletingOrgEligibleRoles.ForEach((deletingOrgEligibleRole) =>
        {
          if (_applicationConfigurationInfo.UserRoleApproval.Enable)
          {
            var pendingRequests = allAccessRolePending.Where(x => x.OrganisationEligibleRoleId == deletingOrgEligibleRole.Id).ToList();
            foreach (var pendingRequest in pendingRequests)
            {
              pendingRequest.IsDeleted = true;
              pendingRequest.Status = (int)UserPendingRoleStaus.Removed;
            }
          }

          deletingOrgEligibleRole.IsDeleted = true;
          rolesRemoved.Append(rolesRemoved.Length > 0 ? "," + deletingOrgEligibleRole.CcsAccessRole.CcsAccessRoleName : deletingOrgEligibleRole.CcsAccessRole.CcsAccessRoleName);

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

      return rolesRemoved.ToString();
    }

    private async Task RemoveUserRoles(List<OrganisationRole> rolesToDelete, Organisation organisation)
    {
      if (rolesToDelete != null && rolesToDelete.Any())
      {
        var deletingRoleIds = rolesToDelete.Select(r => r.RoleId).ToList();

        var userAccessRolesForOrgUsers = await _dataContext.UserAccessRole.Where(uar => !uar.IsDeleted &&
                                           uar.OrganisationEligibleRole.OrganisationId == organisation.Id).ToListAsync();

        var userAccessRolesWithDeletedRoles = userAccessRolesForOrgUsers
          .Where(uar => deletingRoleIds.Contains(uar.OrganisationEligibleRole.CcsAccessRoleId)).ToList();

        userAccessRolesWithDeletedRoles.ForEach((userAccessRolesWithDeletedRole) =>
        {
          userAccessRolesWithDeletedRole.IsDeleted = true;
        });
      }
    }

    private async Task AdminRoleAssignment(Organisation organisation, RoleEligibleTradeType newOrgType, bool autoValidationPassed = false)
    {
      List<User> allAdminsOfOrg = await GetAdminUsers(organisation, false);

      var autoValidationRoles = await _dataContext.AutoValidationRole.ToListAsync();

      if (autoValidationPassed)
      {
        string[] successAdminRoleKeys = null;

        if (newOrgType == RoleEligibleTradeType.Buyer)
        {
          successAdminRoleKeys = _applicationConfigurationInfo.OrgAutoValidation.BuyerSuccessAdminRoles;
        }
        else if (newOrgType == RoleEligibleTradeType.Both)
        {
          successAdminRoleKeys = _applicationConfigurationInfo.OrgAutoValidation.BothSuccessAdminRoles;
        }

        var successAdminRoleIds = await _dataContext.CcsAccessRole.Where(x => successAdminRoleKeys.Contains(x.CcsAccessRoleNameKey)).Select(r => r.Id).ToListAsync();
        autoValidationRoles = autoValidationRoles.Where(x => x.AssignToAdmin == true || successAdminRoleIds.Contains(x.CcsAccessRoleId)).ToList();
      }
      else
      {
        autoValidationRoles = autoValidationRoles.Where(x => x.AssignToAdmin == true).ToList();
      }

      foreach (var role in autoValidationRoles)
      {
        var organisationEligibleRole = organisation.OrganisationEligibleRoles.FirstOrDefault(x => x.CcsAccessRoleId == role.CcsAccessRoleId && !x.IsDeleted);
        var organisationEligibleRoleId = organisationEligibleRole != null ? organisationEligibleRole.Id : 0;

        if (organisationEligibleRoleId <= 0)
        {
          continue;
        }
        // assign roles to all admins
        foreach (var adminDetails in allAdminsOfOrg)
        {
          if (adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == organisationEligibleRoleId && !x.IsDeleted))
          {
            continue;
          }
          var IsRoleValid = RoleApprovalRequiredCheck(organisation, role, adminDetails);
          if (IsRoleValid)
          {
            var defaultUserRole = new UserAccessRole
            {
              OrganisationEligibleRoleId = organisationEligibleRoleId,
              OrganisationEligibleRole = organisationEligibleRole
            };
            adminDetails.UserAccessRoles.Add(defaultUserRole);
          }
          else
          {
            await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(new UserProfileEditRequestInfo
            {
              UserName = adminDetails.UserName,
              OrganisationId = organisation.CiiOrganisationId,
              Detail = new UserRequestDetail
              {
                RoleIds = new List<int> { organisationEligibleRoleId }
              }
            }, sendEmailNotification: false);
          }
        }
      }

      if (_applicationConfigurationInfo.UserRoleApproval.Enable && _applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        foreach (var adminDetails in allAdminsOfOrg)
        {
          if (adminDetails.UserName.ToLower().Split('@')?[1] != organisation.DomainName?.ToLower())
          {
            var servicesWithApprovalRequiredRole = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsWithApprovalRequiredRoleAsync();

            foreach (var approvalRoleService in servicesWithApprovalRequiredRole)
            {
              // Remove all the roles of approval required service except approval required role.
              // All roles of approval required service will be assigned once approval required role is approved.
              var removeCcsRoles = approvalRoleService.CcsServiceRoleMappings.Where(x => x.CcsAccessRole.ApprovalRequired != 1).Select(x => x.CcsAccessRoleId).ToList();
              adminDetails.UserAccessRoles.RemoveAll(x => removeCcsRoles.Contains(x.OrganisationEligibleRole.CcsAccessRoleId));
            }
          }
        }
      }

    }

    private bool RoleApprovalRequiredCheck(Organisation organisation, AutoValidationRole role, User adminDetails)
    {
      return (!_applicationConfigurationInfo.UserRoleApproval.Enable ||
                  role.CcsAccessRole.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalNotRequired ||
                  adminDetails.UserName.ToLower().Split('@')?[1] == organisation.DomainName?.ToLower());
    }
    private bool RoleApprovalRequiredCheck(Organisation organisation, int approvalRequired, User adminDetails)
    {
      return (!_applicationConfigurationInfo.UserRoleApproval.Enable ||
                  approvalRequired == (int)RoleApprovalRequiredStatus.ApprovalNotRequired ||
                  adminDetails.UserName.ToLower().Split('@')?[1] == organisation.DomainName?.ToLower());
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
      List<User> allActiveAdminsOfOrg = await GetAdminUsers(organisation, true);

      foreach (var admin in allActiveAdminsOfOrg)
      {
        // email all admins to notify org type change to supplier and right to buy is removed
        await _ccsSsoEmailService.SendOrgBuyerStatusChangeUpdateToAllAdminsAsync(admin.UserName);
      }
    }

    private async Task<List<User>> GetAdminUsers(Organisation organisation, bool isVerifiedOnly)
    {
      // TODO: find better way to get all admin
      // get all admins
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
      .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisation.Id && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;

      var allAdminsOfOrg = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
        .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Where(u => u.Party.Person.Organisation.CiiOrganisationId == organisation.CiiOrganisationId && u.UserType == UserType.Primary &&
          u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId)
          && (!isVerifiedOnly || u.AccountVerified)
          && !u.IsDeleted)
        .ToListAsync();
      return allAdminsOfOrg;
    }

    private async Task ManualValidateDecline(Organisation organisation, User actionedBy)
    {
      Guid groupId = Guid.NewGuid();

      List<OrganisationAuditEventInfo> auditEventLogs = new();

      if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrganisationTypeBuyerToSupplier, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      }
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both)
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrganisationTypeBothToSupplier, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      }

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.ManualDeclineRightToBuy, groupId, organisation.Id, "", null, actionedBy: actionedBy));

      organisation.RightToBuy = false;
      organisation.SupplierBuyerType = (int)RoleEligibleTradeType.Supplier;

      var organisationAuditInfo = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.ManuallyDecliend,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Admin.ToString(),
        ActionedBy = actionedBy?.UserName
      };

      string rolesAsssignToOrg = await ManualValidateOrgRoleAssignmentAsync(organisation);

      if (!string.IsNullOrWhiteSpace(rolesAsssignToOrg))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, "", rolesAsssignToOrg, actionedBy: actionedBy));
      }

      await _organisationAuditService.UpdateOrganisationAuditAsync(organisationAuditInfo);

      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);

      try
      {
        await _dataContext.SaveChangesAsync();

        List<User> allAdminsOfOrg = await GetAdminUsers(organisation, false);

        foreach (var admin in allAdminsOfOrg)
        {
          await _ccsSsoEmailService.SendOrgDeclineRightToBuyStatusToAllAdminsAsync(admin.UserName);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }
    }

    private async Task ManualValidateApprove(Organisation organisation, User actionedBy)
    {
      Guid groupId = Guid.NewGuid();

      List<OrganisationAuditEventInfo> auditEventLogs = new();

      List<User> allAdminsOfOrg = await GetAdminUsers(organisation, false);

      string rolesAsssignToOrg = await ManualValidateOrgRoleAssignmentAsync(organisation);
      await ManualValidateAdminRoleAssignmentAsync(organisation, allAdminsOfOrg);

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.ManualAcceptationRightToBuy, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      if (!string.IsNullOrWhiteSpace(rolesAsssignToOrg))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, "", rolesAsssignToOrg, actionedBy: actionedBy));
      }

      organisation.RightToBuy = true;

      var organisationAuditInfo = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.ManuallyApproved,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Admin.ToString(),
        ActionedBy = actionedBy?.UserName
      };

      await _organisationAuditService.UpdateOrganisationAuditAsync(organisationAuditInfo);

      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);

      try
      {
        await _dataContext.SaveChangesAsync();

        foreach (var admin in allAdminsOfOrg)
        {
          await _ccsSsoEmailService.SendOrgApproveRightToBuyStatusToAllAdminsAsync(admin.UserName);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }

    }

    private async Task ManualValidateRemove(Organisation organisation, User actionedBy)
    {
      Guid groupId = Guid.NewGuid();

      List<OrganisationAuditEventInfo> auditEventLogs = new();

      List<User> allAdminsOfOrg = await GetAdminUsers(organisation, false);

      string rolesUnassignedToOrg = await ManualValidateOrgAndUsersRoleUnassignmentAsync(organisation);

      await ManualValidateUsersRoleUnassignmentAsync(organisation);

      auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.ManualRemoveRightToBuy, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Buyer)
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrganisationTypeBuyerToSupplier, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      }
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both)
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrganisationTypeBothToSupplier, groupId, organisation.Id, "", null, actionedBy: actionedBy));
      }

      if (!string.IsNullOrWhiteSpace(rolesUnassignedToOrg))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleUnassigned, groupId, organisation.Id, "", rolesUnassignedToOrg, actionedBy: actionedBy));
      }

      organisation.RightToBuy = false;
      organisation.SupplierBuyerType = (int)RoleEligibleTradeType.Supplier;

      string rolesAsssignToOrg = await ManualValidateOrgRoleAssignmentAsync(organisation);
      await ManualValidateAdminRoleAssignmentAsync(organisation, allAdminsOfOrg);

      if (!string.IsNullOrWhiteSpace(rolesAsssignToOrg))
      {
        auditEventLogs.Add(CreateAutoValidationEventLog(OrganisationAuditActionType.Admin, OrganisationAuditEventType.OrgRoleAssigned, groupId, organisation.Id, "", rolesAsssignToOrg, actionedBy: actionedBy));
      }

      var organisationAuditInfo = new OrganisationAuditInfo
      {
        Status = OrgAutoValidationStatus.ManuallyDecliend,
        OrganisationId = organisation.Id,
        Actioned = OrganisationAuditActionType.Admin.ToString(),
        ActionedBy = actionedBy?.UserName
      };

      await _organisationAuditService.UpdateOrganisationAuditAsync(organisationAuditInfo);

      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(auditEventLogs);

      try
      {
        await _dataContext.SaveChangesAsync();

        foreach (var admin in allAdminsOfOrg)
        {
          await _ccsSsoEmailService.SendOrgRemoveRightToBuyStatusToAllAdminsAsync(admin.UserName);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        throw;
      }

    }

    private async Task<string> ManualValidateOrgAndUsersRoleUnassignmentAsync(Organisation organisation)
    {
      var defaultOrgRoles = await _dataContext.AutoValidationRole.Include(x => x.CcsAccessRole)
        .Where(ar => !ar.CcsAccessRole.IsDeleted && !ar.IsSupplier).ToListAsync();

      List<OrganisationRole> rolesToDelete = new List<OrganisationRole>();

      foreach (var defaultOrgRole in defaultOrgRoles)
      {
        rolesToDelete.Add(new OrganisationRole
        {
          RoleId = defaultOrgRole.CcsAccessRoleId,
        });
      }

      string rolesUnassigned = await RemoveOrgRoles(rolesToDelete, organisation);
      return rolesUnassigned;
    }

    private async Task ManualValidateUsersRoleUnassignmentAsync(Organisation organisation)
    {
      var defaultOrgRoles = await _dataContext.AutoValidationRole.Include(x => x.CcsAccessRole)
        .Where(ar => !ar.CcsAccessRole.IsDeleted && ar.IsSupplier && !ar.AssignToAdmin).ToListAsync();

      List<OrganisationRole> rolesToDelete = new List<OrganisationRole>();

      foreach (var defaultOrgRole in defaultOrgRoles)
      {
        rolesToDelete.Add(new OrganisationRole
        {
          RoleId = defaultOrgRole.CcsAccessRoleId,
        });
      }

      await RemoveUserRoles(rolesToDelete, organisation);
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
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Supplier)
      {
        defaultOrgRoles = defaultOrgRoles.Where(o => o.IsSupplier == true).ToList();
      }

      var organisationAudit = _dataContext.OrganisationAudit.FirstOrDefault(x => x.OrganisationId == organisation.Id);
      var isManualPending = organisationAudit != null && organisationAudit.Status == OrgAutoValidationStatus.ManualPending ? true : false;
      var organisationEligiblePendingRoles = _dataContext.OrganisationEligibleRolePending.Where(x => !x.IsDeleted && x.OrganisationId == organisation.Id).ToList();

      if (organisationEligiblePendingRoles != null && organisationEligiblePendingRoles.Count > 0)
      {
        if (isManualPending && organisation.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier)
        {
          var organisationEligiblePendingRoleIds = organisationEligiblePendingRoles.Select(x => x.CcsAccessRoleId).ToList();

          defaultOrgRoles = defaultOrgRoles.Where(x => organisationEligiblePendingRoleIds.Contains(x.CcsAccessRoleId)).ToList();
        }

        organisationEligiblePendingRoles.ForEach((organisationEligiblePendingRole) =>
        {
          organisationEligiblePendingRole.IsDeleted = true;
        });
      }

      StringBuilder rolesAssigned = new();
      foreach (var role in defaultOrgRoles.Select(x => x.CcsAccessRole))
      {
        if (!organisation.OrganisationEligibleRoles.Any(x => x.CcsAccessRoleId == role.Id && !x.IsDeleted))
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

    private async Task ManualValidateAdminRoleAssignmentAsync(Organisation organisation, List<User> allAdminsOfOrg)
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
      else if (organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Supplier)
      {
        defaultAdminRoles = defaultAdminRoles.Where(o => o.IsSupplier == true).ToList();
      }

      var roleIds = defaultAdminRoles.Select(x => x.CcsAccessRoleId);

      if (organisation.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier)
      {
        var successAdminRoleKeys = organisation.SupplierBuyerType == (int)RoleEligibleTradeType.Both ? _applicationConfigurationInfo.OrgAutoValidation.BothSuccessAdminRoles : _applicationConfigurationInfo.OrgAutoValidation.BuyerSuccessAdminRoles;
        var successAdminRoleIds = await _dataContext.CcsAccessRole.Where(x => successAdminRoleKeys.Contains(x.CcsAccessRoleNameKey)).Select(r => r.Id).ToListAsync();
        roleIds = roleIds.Union(successAdminRoleIds);
      }

      var defaultRoles = await _dataContext.OrganisationEligibleRole
            .Where(r => r.Organisation.CiiOrganisationId == organisation.CiiOrganisationId && !r.IsDeleted &&
            roleIds.Contains(r.CcsAccessRoleId))
            .ToListAsync();

      var servicesWithApprovalRequiredRole = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsWithApprovalRequiredRoleAsync();

      foreach (var role in defaultRoles)
      {
        await AssignRoleToAllOrgAdmins(role, allAdminsOfOrg, organisation, servicesWithApprovalRequiredRole);
      }

      await _dataContext.SaveChangesAsync();
    }

    private async Task AssignRoleToAllOrgAdmins(OrganisationEligibleRole role, List<User> allAdminsOfOrg, Organisation organisation, List<CcsServiceRoleGroup> servicesWithApprovalRequiredRole) 
    {
      foreach (var adminDetails in allAdminsOfOrg)
      {
        if (!adminDetails.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == role.Id && !x.IsDeleted))
        {
          var isAdminDomainSameAsOrg = adminDetails.UserName.ToLower().Split('@')?[1] == organisation.DomainName?.ToLower();
          
          // Remove normals roles which are part of service which required role approval
          // They will be assigned together with role approval.
          if (_applicationConfigurationInfo.UserRoleApproval.Enable && _applicationConfigurationInfo.ServiceRoleGroupSettings.Enable &&
            !isAdminDomainSameAsOrg && RoleBelongToApprovalRequiredService(role, servicesWithApprovalRequiredRole)) 
          {
            continue;
          }

          if (!_applicationConfigurationInfo.UserRoleApproval.Enable || role.CcsAccessRole.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalNotRequired || 
              isAdminDomainSameAsOrg)
          {
            var defaultUserRole = new UserAccessRole
            {
              OrganisationEligibleRoleId = role.Id
            };
            adminDetails.UserAccessRoles.Add(defaultUserRole);  
          }
          else
          {
            await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(new UserProfileEditRequestInfo
            {
              UserName = adminDetails.UserName,
              OrganisationId = organisation.CiiOrganisationId,
              Detail = new UserRequestDetail
              {
                RoleIds = new List<int> { role.Id }
              }
            }, sendEmailNotification: false);
          }

        }
      }
      await _dataContext.SaveChangesAsync();
    }

    #endregion

    #region ServiceRoleGroup
    public async Task<List<ServiceRoleGroup>> GetOrganisationServiceRoleGroupsAsync(string ciiOrganisationId)
    {
      if (!_applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      if (!ValidateCiiOrganisationID(ciiOrganisationId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      var orgRoles = await GetOrganisationRolesAsync(ciiOrganisationId);
      var serviceRoleGroupsEntity = await _rolesToServiceRoleGroupMapperService.OrgRolesToServiceRoleGroupsAsync(orgRoles.Select(x => x.RoleId).ToList());
      var serviceRoleGroups = serviceRoleGroupsEntity.Select(x => new ServiceRoleGroup
                              {
                                Id = x.Id,
                                Key = x.Key,
                                Name = x.Name,
                                OrgTypeEligibility = x.OrgTypeEligibility,
                                SubscriptionTypeEligibility = x.SubscriptionTypeEligibility,
                                TradeEligibility = x.TradeEligibility,
                                DisplayOrder = x.DisplayOrder,
                                Description = x.Description
                              }).ToList();
      return serviceRoleGroups;
    }

    public async Task UpdateOrganisationEligibleServiceRoleGroupsAsync(string ciiOrganisationId, bool isBuyer, List<int> serviceRoleGroupsToAdd, List<int> serviceRoleGroupsToDelete)
    {
      if (!_applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      if (!ValidateCiiOrganisationID(ciiOrganisationId) || serviceRoleGroupsToAdd == null || serviceRoleGroupsToDelete == null) 
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      var ccsAccessRolesToAdd = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsToCcsRolesAsync(serviceRoleGroupsToAdd);
      var ccsAccessRolesToDelete = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsToCcsRolesAsync(serviceRoleGroupsToDelete);

      var addRoles = ccsAccessRolesToAdd.Select(r => new OrganisationRole { RoleId = r.Id, RoleKey = r.CcsAccessRoleNameKey, RoleName = r.CcsAccessRoleName }).Distinct().ToList();
      var deleteRoles = ccsAccessRolesToDelete.Select(r => new OrganisationRole { RoleId = r.Id, RoleKey = r.CcsAccessRoleNameKey, RoleName = r.CcsAccessRoleName }).Distinct().ToList();

      await UpdateOrganisationEligibleRolesAsync(ciiOrganisationId, isBuyer, addRoles, deleteRoles);
    }

    public async Task UpdateOrgAutoValidServiceRoleGroupsAsync(string ciiOrganisationId, RoleEligibleTradeType newOrgType, List<int> serviceRoleGroupsToAdd, List<int> serviceRoleGroupsToDelete, List<int> serviceRoleGroupsToAutoValid, string? companyHouseId) 
    {
      if (!_applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      if (!ValidateCiiOrganisationID(ciiOrganisationId) || !Enum.IsDefined(typeof(RoleEligibleTradeType), newOrgType) || serviceRoleGroupsToAdd == null || serviceRoleGroupsToDelete == null || serviceRoleGroupsToAutoValid == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }

      var ccsAccessRolesToAdd = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsToCcsRolesAsync(serviceRoleGroupsToAdd);
      var ccsAccessRolesToDelete = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsToCcsRolesAsync(serviceRoleGroupsToDelete);
      var ccsAccessRolesAutoValidRoles = await _rolesToServiceRoleGroupMapperService.ServiceRoleGroupsToCcsRolesAsync(serviceRoleGroupsToAutoValid);

      var addRoles = ccsAccessRolesToAdd.Select(r => new OrganisationRole { RoleId = r.Id, RoleKey = r.CcsAccessRoleNameKey, RoleName = r.CcsAccessRoleName }).Distinct().ToList();
      var deleteRoles = ccsAccessRolesToDelete.Select(r => new OrganisationRole { RoleId = r.Id, RoleKey = r.CcsAccessRoleNameKey, RoleName = r.CcsAccessRoleName }).Distinct().ToList();
      var autoValidRoles = ccsAccessRolesAutoValidRoles.Select(r => new OrganisationRole { RoleId = r.Id, RoleKey = r.CcsAccessRoleNameKey, RoleName = r.CcsAccessRoleName }).Distinct().ToList();

      await UpdateOrgAutoValidationEligibleRolesAsync(ciiOrganisationId, newOrgType, addRoles, deleteRoles, autoValidRoles, companyHouseId);
    }

    private static bool RoleBelongToApprovalRequiredService(OrganisationEligibleRole role, List<CcsServiceRoleGroup> servicesWithApprovalRequiredRole) 
    {
      foreach (var approvalRoleService in servicesWithApprovalRequiredRole)
      {
        var removeRoles = approvalRoleService.CcsServiceRoleMappings.Where(x => x.CcsAccessRole.ApprovalRequired != 1).Select(x => x.CcsAccessRoleId).ToList();
        // Return true for normal role that belongs to approval required service 
        if (removeRoles.Any(x => x == role.CcsAccessRoleId)) 
        {
          return true;
        }
      }
      return false;
    }

    #endregion

  }
}
