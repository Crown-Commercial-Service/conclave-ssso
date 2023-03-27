using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class UserProfileService : IUserProfileService
  {
    private readonly IDataContext _dataContext;
    private readonly IUserProfileHelperService _userHelper;
    private readonly RequestContext _requestContext;
    private readonly IIdamService _idamService;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly IAdaptorNotificationService _adapterNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly IAuditLoginService _auditLoginService;
    private readonly IRemoteCacheService _remoteCacheService;
    private readonly ICacheInvalidateService _cacheInvalidateService;
    private readonly ICryptographyService _cryptographyService;
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly ILookUpService _lookUpService;
    private readonly IWrapperApiService _wrapperApiService;
    private readonly IUserProfileRoleApprovalService _userProfileRoleApprovalService;
    private readonly IServiceRoleGroupMapperService _serviceRoleGroupMapperService;
    private readonly IOrganisationGroupService _organisationGroupService;

    public UserProfileService(IDataContext dataContext, IUserProfileHelperService userHelper,
      RequestContext requestContext, IIdamService idamService, ICcsSsoEmailService ccsSsoEmailService,
      IAdaptorNotificationService adapterNotificationService, IWrapperCacheService wrapperCacheService,
      IAuditLoginService auditLoginService, IRemoteCacheService remoteCacheService,
      ICacheInvalidateService cacheInvalidateService, ICryptographyService cryptographyService,
      ApplicationConfigurationInfo appConfigInfo, ILookUpService lookUpService, IWrapperApiService wrapperApiService,
      IUserProfileRoleApprovalService userProfileRoleApprovalService, IServiceRoleGroupMapperService serviceRoleGroupMapperService,
      IOrganisationGroupService organisationGroupService)
    {
      _dataContext = dataContext;
      _userHelper = userHelper;
      _requestContext = requestContext;
      _idamService = idamService;
      _ccsSsoEmailService = ccsSsoEmailService;
      _adapterNotificationService = adapterNotificationService;
      _wrapperCacheService = wrapperCacheService;
      _auditLoginService = auditLoginService;
      _remoteCacheService = remoteCacheService;
      _cacheInvalidateService = cacheInvalidateService;
      _cryptographyService = cryptographyService;
      _appConfigInfo = appConfigInfo;
      _lookUpService = lookUpService;
      _wrapperApiService = wrapperApiService;
      _userProfileRoleApprovalService = userProfileRoleApprovalService;
      _serviceRoleGroupMapperService = serviceRoleGroupMapperService;
      _organisationGroupService = organisationGroupService;
    }

    public async Task<UserEditResponseInfo> CreateUserAsync(UserProfileEditRequestInfo userProfileRequestInfo, bool isNewOrgAdmin = false)
    {
      var isRegisteredInIdam = false;
      var userName = userProfileRequestInfo.UserName.ToLower();
      _userHelper.ValidateUserName(userName);

      var organisation = await _dataContext.Organisation
        .Include(o => o.UserGroups).ThenInclude(ge => ge.GroupEligibleRoles)
        .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
        .Include(o => o.OrganisationEligibleIdentityProviders)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);
      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var user = await _dataContext.User
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user != null)
      {
        throw new ResourceAlreadyExistsException();
      }

      Validate(userProfileRequestInfo, false, organisation);

      var eligibleIdentityProviders = await _dataContext.OrganisationEligibleIdentityProvider
        .Include(x => x.IdentityProvider)
        .Where(i => !i.IsDeleted && userProfileRequestInfo.Detail.IdentityProviderIds.Contains(i.Id) &&
                    i.Organisation.Id == organisation.Id).ToListAsync();

      var isConclaveConnectionIncluded = eligibleIdentityProviders.Any(idp => idp.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
      var isNonUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => eligibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName));

      // This is to enforce MFA over any other
      //if (userProfileRequestInfo.MfaEnabled && isNonUserNamePwdConnectionIncluded)
      //{
      //  throw new CcsSsoException(ErrorConstant.ErrorMfaFlagForInvalidConnection);
      //}

      //validate mfa and assign mfa if user is part of any admin role or group
      if (!userProfileRequestInfo.MfaEnabled && isConclaveConnectionIncluded)
      {
        var partOfAdminRole = organisation.OrganisationEligibleRoles.Any(r => userProfileRequestInfo.Detail.RoleIds != null && userProfileRequestInfo.Detail.RoleIds.Any(role => role == r.Id) && r.MfaEnabled);
        if (partOfAdminRole)
        {
          throw new CcsSsoException(ErrorConstant.ErrorMfaFlagRequired);
        }
        else
        {
          var partOfAdminGroup = organisation.UserGroups.Any(oug => userProfileRequestInfo.Detail.GroupIds != null && userProfileRequestInfo.Detail.GroupIds.Contains(oug.Id) && !oug.IsDeleted &&
                              oug.GroupEligibleRoles.Any(er => !er.IsDeleted && er.OrganisationEligibleRole.MfaEnabled));
          if (partOfAdminGroup)
          {
            throw new CcsSsoException(ErrorConstant.ErrorMfaFlagRequired);
          }
        }
      }


      // Set user groups
      var userGroupMemberships = new List<UserGroupMembership>();
      userProfileRequestInfo.Detail.GroupIds?.ForEach((groupId) =>
      {
        userGroupMemberships.Add(new UserGroupMembership
        {
          OrganisationUserGroupId = groupId
        });
      });

      // Set user roles
      var userAccessRoles = new List<UserAccessRole>();
      var userAccessRoleRequiredApproval = new List<int>();

      // #Auto validation role assignment will not be applicable here if auto validation on. Role assignment will be done as part of auto validation
      if (!_appConfigInfo.OrgAutoValidation.Enable || !isNewOrgAdmin)
      {
        var ccsAccessRoleRequiredApproval = await _dataContext.CcsAccessRole.Where(x => x.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired).ToListAsync();

        userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
        {
          var ccsAccessRoleId = organisation.OrganisationEligibleRoles.FirstOrDefault(x => x.Id == roleId)?.CcsAccessRoleId;
          var isRoleRequiredApproval = ccsAccessRoleId != null && ccsAccessRoleRequiredApproval != null && ccsAccessRoleRequiredApproval.Any(x => x.Id == ccsAccessRoleId);
          var isUserDomainValid = userName?.ToLower().Split('@')?[1] == organisation.DomainName?.ToLower();

          if (_appConfigInfo.UserRoleApproval.Enable && !isUserDomainValid && isRoleRequiredApproval)
          {
            userAccessRoleRequiredApproval.Add(roleId);
          }
          else
          {
            userAccessRoles.Add(new UserAccessRole
            {
              OrganisationEligibleRoleId = roleId
            });
          }
        });

        var defaultUserRoleId = organisation.OrganisationEligibleRoles.First(or => or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.DefaultUserRoleNameKey).Id;

        // Set default user role if no role available
        if (userProfileRequestInfo.Detail.RoleIds == null || !userProfileRequestInfo.Detail.RoleIds.Any() || !userAccessRoles.Exists(ur => ur.OrganisationEligibleRoleId == defaultUserRoleId))
        {
          userAccessRoles.Add(new UserAccessRole
          {
            OrganisationEligibleRoleId = defaultUserRoleId
          });
        }
      }

      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(p => p.PartyTypeName == PartyTypeName.User)).Id;

      var party = new Party
      {
        PartyTypeId = partyTypeId,
        Person = new Person
        {
          FirstName = userProfileRequestInfo.FirstName.Trim(),
          LastName = userProfileRequestInfo.LastName.Trim(),
          OrganisationId = organisation.Id
        },
        User = new User
        {
          UserName = userName,
          UserTitle = (int)Enum.Parse(typeof(UserTitle), string.IsNullOrWhiteSpace(userProfileRequestInfo.Title) ? "Unspecified" : userProfileRequestInfo.Title),
          UserGroupMemberships = userGroupMemberships,
          UserAccessRoles = userAccessRoles,
          UserIdentityProviders = userProfileRequestInfo.Detail.IdentityProviderIds.Select(idpId => new UserIdentityProvider
          {
            OrganisationEligibleIdentityProviderId = idpId
          }).ToList(),
          MfaEnabled = userProfileRequestInfo.MfaEnabled,
          CcsServiceId = _requestContext.ServiceId > 0 ? _requestContext.ServiceId : null
        }
      };

      _dataContext.Party.Add(party);

      await _dataContext.SaveChangesAsync();

      if (userAccessRoleRequiredApproval.Any())
      {
        await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(new UserProfileEditRequestInfo
        {
          UserName = userName,
          OrganisationId = organisation.CiiOrganisationId,
          Detail = new UserRequestDetail
          {
            RoleIds = userAccessRoleRequiredApproval
          }
        });
      }

      if (isConclaveConnectionIncluded)
      {
        SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
        {
          Email = userName,
          Password = userProfileRequestInfo.Password,
          SendUserRegistrationEmail = false, // Emails are sent separately along with other IDP selections
          UserName = userName,
          FirstName = userProfileRequestInfo.FirstName,
          LastName = userProfileRequestInfo.LastName,
          MfaEnabled = userProfileRequestInfo.MfaEnabled
        };

        try
        {
          await _idamService.RegisterUserInIdamAsync(securityApiUserInfo);
          isRegisteredInIdam = true;
        }
        catch (Exception)
        {
          // If Idam registration failed, remove the user record in DB
          _dataContext.Party.Remove(party);
          await _dataContext.SaveChangesAsync();
          throw;
        }
      }

      if (userProfileRequestInfo.SendUserRegistrationEmail)
      {
        var isAutovalidationSuccess = false;
        // #Auto validation
        if (isNewOrgAdmin && _appConfigInfo.OrgAutoValidation.Enable)
        {
          //bool isDomainValidForAutoValidation = false;
          //try
          //{
          //  isDomainValidForAutoValidation = await _lookUpService.IsDomainValidForAutoValidation(userName);
          //}
          //catch(Exception ex)
          //{
          //  // TODO: lookup api fail logic
          //  Console.WriteLine(ex.Message);
          //}

          // If auto validation on and user is buyer or both
          var autoValidationDetails = new AutoValidationDetails
          {
            AdminEmailId = userProfileRequestInfo.UserName.ToLower(),
            CompanyHouseId = userProfileRequestInfo.CompanyHouseId
          };

          isAutovalidationSuccess = await _wrapperApiService.PostAsync<bool>($"{userProfileRequestInfo.OrganisationId}/registration", autoValidationDetails, "ERROR_ORGANISATION_AUTOVALIDATION");
        }

        if (isConclaveConnectionIncluded && isNonUserNamePwdConnectionIncluded)
        {
          var activationlink = await _idamService.GetActivationEmailVerificationLink(userName);
          var listOfIdpName = eligibleIdentityProviders.Where(idp => idp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName).Select(y => y.IdentityProvider.IdpName);

          await _ccsSsoEmailService.SendUserConfirmEmailBothIdpAsync(party.User.UserName, string.Join(",", listOfIdpName), activationlink);

        }

        else if (isNonUserNamePwdConnectionIncluded)
        {
          var listOfIdpName = eligibleIdentityProviders.Where(idp => idp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName).Select(y => y.IdentityProvider.IdpName);
          await _ccsSsoEmailService.SendUserConfirmEmailOnlyFederatedIdpAsync(party.User.UserName, string.Join(",", listOfIdpName));

        }
        else if (isConclaveConnectionIncluded)
        {
          // #Auto validation
          string ccsMsg = (isNewOrgAdmin && !isAutovalidationSuccess && organisation.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier) ?
                          "Please note that notification has been sent to CCS to verify the buyer status of your Organisation. " +
                          "You will be informed within the next 24 to 72 hours" : string.Empty;
          var activationlink = await _idamService.GetActivationEmailVerificationLink(userName);
          await _ccsSsoEmailService.SendUserConfirmEmailOnlyUserIdPwdAsync(party.User.UserName, string.Join(",", activationlink), ccsMsg);
        }
      }

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.UserCreate, AuditLogApplication.ManageUserAccount, $"UserId:{party.User.Id}, UserIdpId:{string.Join(",", party.User.UserIdentityProviders.Select(uidp => uidp.OrganisationEligibleIdentityProviderId))}," + " " +
        $"UserGroupIds:{string.Join(",", party.User.UserGroupMemberships.Select(g => g.OrganisationUserGroupId))}," + " " +
        $"UserRoleIds:{string.Join(",", party.User.UserAccessRoles.Select(r => r.OrganisationEligibleRoleId))}");

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrganisationUsers}-{organisation.CiiOrganisationId}");

      // Notify the adapter
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Create, userProfileRequestInfo.UserName, organisation.CiiOrganisationId);

      return new UserEditResponseInfo
      {
        UserId = party.User.UserName,
        IsRegisteredInIdam = isRegisteredInIdam
      };
    }
    // #Delegated
    public async Task<UserProfileResponseInfo> GetUserAsync(string userName, bool isDelegated = false, bool isSearchUser = false, string delegatedOrgId = "")
    {
      User user = null;

      _userHelper.ValidateUserName(userName);

      var users = await _dataContext.User
        .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
        .ThenInclude(oug => oug.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)

        .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)

        .Include(u => u.Party).ThenInclude(p => p.Person)
        .ThenInclude(pr => pr.Organisation)
        .Include(u => u.UserIdentityProviders).ThenInclude(uidp => uidp.OrganisationEligibleIdentityProvider).ThenInclude(oi => oi.IdentityProvider)
        .Include(o => o.OriginOrganization)
        .Where(u => !u.IsDeleted && u.UserName.ToLower() == userName.ToLower()).ToListAsync();

      // Search for user delegation details for org
      if (isDelegated && !string.IsNullOrWhiteSpace(delegatedOrgId))
      {
        user = users.SingleOrDefault(u => u.UserType == DbModel.Constants.UserType.Delegation
               && u.Party.Person.Organisation.CiiOrganisationId == delegatedOrgId
               && !u.IsDeleted
               && u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date);

        // If searching for user to delegate in organisation and already exist
        if (isSearchUser && user != default)
        {
          throw new ResourceAlreadyExistsException();
        }
        // user delegation not exist
        else if (user == default)
        {
          user = users.SingleOrDefault(u => u.UserType == DbModel.Constants.UserType.Primary && u.AccountVerified);
        }
      }
      // User primary org details
      else
      {
        user = users.SingleOrDefault(u => u.UserType == DbModel.Constants.UserType.Primary);
      }

      var userDelegatedOrgs = users.Where(u => u.UserType == DbModel.Constants.UserType.Delegation &&
                              !u.IsDeleted &&
                              u.DelegationStartDate.Value.Date <= DateTime.UtcNow.Date &&
                              u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date &&
                              (!string.IsNullOrWhiteSpace(delegatedOrgId) || u.DelegationAccepted) &&
                              ((isDelegated && string.IsNullOrWhiteSpace(delegatedOrgId)) || u.Party.Person.Organisation.CiiOrganisationId == delegatedOrgId)
                              )
                              .Select(u => new UserDelegationDetails
                              {
                                DelegatedOrgId = u.Party.Person.Organisation.CiiOrganisationId,
                                DelegatedOrgName = u.Party.Person.Organisation.LegalName,
                                StartDate = u.DelegationStartDate,
                                EndDate = u.DelegationEndDate,
                                DelegationAccepted = u.DelegationAccepted
                              }).ToArray();

      if (user != null)
      {
        var userProfileInfo = new UserProfileResponseInfo
        {
          UserName = user.UserName,
          OrganisationId = user.Party.Person.Organisation.CiiOrganisationId,
          OriginOrganisationName = isDelegated ? (user.UserType == DbModel.Constants.UserType.Primary ? user.Party.Person.Organisation.LegalName : user.OriginOrganization?.LegalName) : default,
          FirstName = user.Party.Person.FirstName,
          LastName = (isDelegated || !string.IsNullOrWhiteSpace(delegatedOrgId)) && !user.DelegationAccepted ?
                       user.Party.Person.LastName.Substring(0, 1).PadRight(user.Party.Person.LastName.Length, '*') :
                       user.Party.Person.LastName,
          MfaEnabled = user.MfaEnabled,
          Title = Enum.GetName(typeof(UserTitle), user.UserTitle),
          AccountVerified = user.AccountVerified,
          Detail = new UserResponseDetail
          {
            Id = user.Id,
            CanChangePassword = isDelegated ? default : user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName),
            IdentityProviders = isDelegated ? default : user.UserIdentityProviders.Where(uidp => !uidp.IsDeleted).Select(idp => new UserIdentityProviderInfo
            {
              IdentityProvider = idp.OrganisationEligibleIdentityProvider.IdentityProvider?.IdpConnectionName,
              IdentityProviderId = idp.OrganisationEligibleIdentityProviderId,
              IdentityProviderDisplayName = idp.OrganisationEligibleIdentityProvider.IdentityProvider?.IdpName,
            }).ToList(),
            UserGroups = new List<GroupAccessRole>(),
            RolePermissionInfo = user.UserAccessRoles.Where(uar => !uar.IsDeleted).Select(uar => new RolePermissionInfo
            {
              RoleId = uar.OrganisationEligibleRole.Id,
              RoleKey = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
              RoleName = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
              ServiceClientId = uar.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceClientId,
              ServiceClientName = uar.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName
            }).ToList(),
            // Return organisation list of user's delegation
            DelegatedOrgs = isDelegated ? userDelegatedOrgs : default
          }
        };

        if (user.UserGroupMemberships != null)
        {
          foreach (var userGroupMembership in user.UserGroupMemberships)
          {
            if (!userGroupMembership.IsDeleted && userGroupMembership.OrganisationUserGroup.GroupEligibleRoles != null)
            {

              if (userGroupMembership.OrganisationUserGroup.GroupEligibleRoles.Any())
              {
                // For every role in the group populate the group role info
                foreach (var groupAccess in userGroupMembership.OrganisationUserGroup.GroupEligibleRoles.Where(x => !x.IsDeleted))
                {
                  var groupAccessRole = new GroupAccessRole
                  {
                    GroupId = userGroupMembership.OrganisationUserGroup.Id,
                    Group = userGroupMembership.OrganisationUserGroup.UserGroupName,
                    AccessRoleName = groupAccess.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
                    AccessRole = groupAccess.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
                    ServiceClientId = groupAccess.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceClientId,
                    ServiceClientName = groupAccess.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService.ServiceName
                  };

                  userProfileInfo.Detail.UserGroups.Add(groupAccessRole);
                }
              }
              else // If group doesnt have a role then just send the group with empty role
              {
                var groupAccessRole = new GroupAccessRole
                {
                  GroupId = userGroupMembership.OrganisationUserGroup.Id,
                  Group = userGroupMembership.OrganisationUserGroup.UserGroupName,
                  AccessRoleName = string.Empty,
                  AccessRole = string.Empty,
                };

                userProfileInfo.Detail.UserGroups.Add(groupAccessRole);
              }
            }
          }
        }

        return userProfileInfo;
      }

      throw new ResourceNotFoundException();
    }
    // #Delegated

    public async Task<UserListWithServiceGroupRoleResponse> GetUsersV1Async(string organisationId, ResultSetCriteria resultSetCriteria, UserFilterCriteria userFilterCriteria)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var userListResponse = await GetUsersAsync(organisationId, resultSetCriteria, userFilterCriteria);


      List<UserListWithServiceRoleGroupInfo> userlist = new List<UserListWithServiceRoleGroupInfo>();

      foreach (var user in userListResponse.UserList)
      {
        var roleIds = user.RolePermissionInfo.Select(x => x.RoleId).ToList();
        var serviceRoleGroups = await _serviceRoleGroupMapperService.OrgRolesToServiceRoleGroupsAsync(roleIds);

        List<ServiceRoleGroupInfo> serviceRoleGroupInfo = new List<ServiceRoleGroupInfo>();

        foreach (var serviceRoleGroup in serviceRoleGroups)
        {
          serviceRoleGroupInfo.Add(new ServiceRoleGroupInfo()
          {
            Id = serviceRoleGroup.Id,
            Name = serviceRoleGroup.Name,
            Key = serviceRoleGroup.Key,
          });
        }

        userlist.Add(new UserListWithServiceRoleGroupInfo
        {
          RemainingDays = user.RemainingDays,
          OriginOrganisation = user.OriginOrganisation,
          UserName = user.UserName,
          DelegationAccepted = user.DelegationAccepted,
          EndDate = user.EndDate,
          IsAdmin = user.IsAdmin,
          Name = user.Name,
          StartDate = user.StartDate,
          ServicePermissionInfo = serviceRoleGroupInfo

        });

      }

      return new UserListWithServiceGroupRoleResponse
      {
        CurrentPage = userListResponse.CurrentPage,
        PageCount = userListResponse.PageCount,
        RowCount = userListResponse.RowCount,
        OrganisationId = userListResponse.OrganisationId,
        UserList = userlist
      };


    }
    public async Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, UserFilterCriteria userFilterCriteria)
    {

      var apiKey = _appConfigInfo.ApiKey;
      var apiKeyInRequest = _requestContext.apiKey;

      if (apiKeyInRequest != null)
      {
        if (apiKey != apiKeyInRequest)
        {
          throw new ForbiddenException();
        }
      }
      else if (_requestContext.Roles != null)
      {
        if ((_requestContext.Roles.Count == 1 && _requestContext.Roles.Contains("ORG_DEFAULT_USER")) && !userFilterCriteria.isAdmin)
        {
          throw new ForbiddenException();
        }
      }
      else
      {
        throw new ForbiddenException();
      }

      string searchString = userFilterCriteria.searchString;
      bool includeSelf = userFilterCriteria.includeSelf;
      bool isDelegatedOnly = userFilterCriteria.isDelegatedOnly;
      bool isDelegatedExpiredOnly = userFilterCriteria.isDelegatedExpiredOnly;
      bool isAdmin = userFilterCriteria.isAdmin;

      var organisation = await _dataContext.Organisation.FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == organisationId);


      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var searchFirstNameLowerCase = string.Empty;
      var searchLastNameLowerCase = string.Empty;
      var havingMultipleWords = false;

      if (!string.IsNullOrWhiteSpace(searchString))
      {
        searchString = searchString.Trim().ToLower();
        var searchStringArray = searchString.Split(" ");
        searchFirstNameLowerCase = searchStringArray[0];

        if (searchStringArray.Length > 1)
        {
          havingMultipleWords = true;
          searchLastNameLowerCase = searchStringArray[1];
        }
      }

      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
      .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisation.Id && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;


      var userTypeSearch = isDelegatedOnly ? DbModel.Constants.UserType.Delegation : DbModel.Constants.UserType.Primary;

      var userQuery = _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
        .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Include(o => o.OriginOrganization)
        // Include deleted for delegated expired
        .Where(u => u.Party.Person.Organisation.CiiOrganisationId == organisationId && u.UserType == userTypeSearch);

      if (!isDelegatedExpiredOnly)
        userQuery = userQuery.Where(u => !u.IsDeleted);

      if (!includeSelf)
        userQuery = userQuery.Where(u => u.Id != _requestContext.UserId);
      // #Autovalidation
      if (isAdmin && userFilterCriteria.includeUnverifiedAdmin)
        userQuery = userQuery.Where(u => u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId) && !u.IsDeleted);
      else if (isAdmin)
        userQuery = userQuery.Where(u => u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId) && u.AccountVerified && !u.IsDeleted);


      // Delegated and delegated expired conditions
      if (isDelegatedOnly)
        userQuery = userQuery.Where(u => isDelegatedExpiredOnly ? u.DelegationEndDate.Value.Date <= DateTime.UtcNow.Date :
                              u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date);


      if (!string.IsNullOrWhiteSpace(searchString))
      {
        userQuery = userQuery.Where(u => u.UserName.ToLower().Contains(searchString)
        ||
            // Delegation search and delegation not accepted then don't search in last name
            (havingMultipleWords && u.Party.Person.FirstName.ToLower().Contains(searchFirstNameLowerCase) &&
              (!isDelegatedOnly ? u.Party.Person.LastName.ToLower().Contains(searchLastNameLowerCase) :
                u.DelegationAccepted && u.Party.Person.LastName.ToLower().Contains(searchLastNameLowerCase)
              ) ||
            // Allow searching for orign org in delegation
            (isDelegatedOnly && u.OriginOrganization.LegalName.ToLower().Contains(searchString))
            )
        || (!havingMultipleWords &&
            (u.Party.Person.FirstName.ToLower().Contains(searchString) ||
              (!isDelegatedOnly ? u.Party.Person.LastName.ToLower().Contains(searchString) :
                                  u.DelegationAccepted && u.Party.Person.LastName.ToLower().Contains(searchString)) ||
            // Allow searching for orign org in delegation
            (isDelegatedOnly && u.OriginOrganization.LegalName.ToLower().Contains(searchString))
            )
           )
        );
      }

      userQuery = userQuery.OrderBy(u => u.Party.Person.FirstName).ThenBy(u => u.Party.Person.LastName);


      var userPagedInfo = await _dataContext.GetPagedResultAsync(userQuery, resultSetCriteria);

      var userListResponse = new UserListResponse
      {
        OrganisationId = organisationId,
        CurrentPage = userPagedInfo.CurrentPage,
        PageCount = userPagedInfo.PageCount,
        RowCount = userPagedInfo.RowCount,
        UserList = userPagedInfo.Results != null ? userPagedInfo.Results.Select(up => new UserListInfo
        {
          Name = isDelegatedOnly && !up.DelegationAccepted ? $"{up.Party.Person.FirstName} " +
                   $"{up.Party.Person.LastName.Substring(0, 1).PadRight(up.Party.Person.LastName.Length, '*')}" :
                   $"{up.Party.Person.FirstName} {up.Party.Person.LastName}",
          UserName = up.UserName,
          // Delegation specific fields
          StartDate = isDelegatedOnly ? up.DelegationStartDate : default,
          EndDate = isDelegatedOnly ? up.DelegationEndDate : default,
          RemainingDays = !isDelegatedOnly || isDelegatedExpiredOnly || up.DelegationStartDate is null ? 0 : Convert.ToInt32((up.DelegationEndDate.Value - up.DelegationStartDate.Value).Days),
          OriginOrganisation = !isDelegatedOnly ? default : up.OriginOrganization?.LegalName,
          DelegationAccepted = !isDelegatedOnly ? default : up.DelegationAccepted,
          RolePermissionInfo = !isDelegatedOnly ? default : up.UserAccessRoles.Select(uar => new RolePermissionInfo
          {
            RoleId = uar.OrganisationEligibleRole.Id,
            RoleKey = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
            RoleName = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
          }).ToList(),
          IsAdmin = up.UserAccessRoles.Any(x => !x.IsDeleted && x.OrganisationEligibleRoleId == orgAdminAccessRoleId && !x.OrganisationEligibleRole.IsDeleted),
        }).ToList() : new List<UserListInfo>()
      };

      return userListResponse;
    }


    public async Task<AdminUserListResponse> GetAdminUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria)
    {
      if (!await _dataContext.Organisation.AnyAsync(o => !o.IsDeleted && o.CiiOrganisationId == organisationId))
      {
        throw new ResourceNotFoundException();
      }
      var Id = (await _dataContext.Organisation.FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == organisationId)).Id;

      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
      .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == Id && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;

      var userPagedInfo = await _dataContext.GetPagedResultAsync(_dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .Include(u => u.UserAccessRoles)
        .Where(u => !u.IsDeleted &&
        u.Party.Person.Organisation.CiiOrganisationId == organisationId && u.AccountVerified == true &&
        u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId))
        .OrderBy(u => u.Party.Person.FirstName).ThenBy(u => u.Party.Person.LastName), resultSetCriteria);

      var UserListResponse = new AdminUserListResponse
      {
        OrganisationId = organisationId,
        CurrentPage = userPagedInfo.CurrentPage,
        PageCount = userPagedInfo.PageCount,
        RowCount = userPagedInfo.RowCount,
        AdminUserList = userPagedInfo.Results != null ? userPagedInfo.Results.Select(up => new AdminUserListInfo
        {
          FirstName = up.Party.Person.FirstName,
          LastName = up.Party.Person.LastName,
          Email = up.UserName,
          Role = "Admin"
        }).ToList() : new List<AdminUserListInfo>()
      };

      return UserListResponse;
    }

    public async Task DeleteUserAsync(string userName, bool checkForLastAdmin = true)
    {

      _userHelper.ValidateUserName(userName);

      var users = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
        .Include(u => u.UserGroupMemberships)
        .Include(u => u.UserAccessRoles)
        .Include(u => u.UserIdentityProviders)
        .Include(u => u.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses) // Get virtual addresses
        .Include(u => u.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.ContactPoints) // Assigned contact points
        .Where(u => !u.IsDeleted && u.UserName == userName).ToListAsync();

      if (users == null || !users.Any())
      {
        throw new ResourceNotFoundException();
      }

      var primaryUser = users.SingleOrDefault(x => x.UserType == DbModel.Constants.UserType.Primary);

      if (checkForLastAdmin && await IsOrganisationOnlyAdminAsync(primaryUser, userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorCannotDeleteLastOrgAdmin);
      }

      List<int> deletingContactPointIds = new();
      if (primaryUser.Party.ContactPoints != null)
      {
        primaryUser.Party.ContactPoints.ForEach((cp) =>
        {
          cp.IsDeleted = true;
          deletingContactPointIds.Add(cp.Id);
          if (cp.ContactDetail != null)
          {
            cp.ContactDetail.IsDeleted = true;
            cp.ContactDetail.VirtualAddresses.ForEach((va) => { va.IsDeleted = true; });
            cp.ContactDetail.ContactPoints.ForEach((otherContactPoint) => { otherContactPoint.IsDeleted = true; }); // Delete assigned contacts
          }
        });
      }

      // #Delegated delete all delegated as well
      foreach (var user in users)
      {
        user.IsDeleted = true;
        user.Party.IsDeleted = true;
        user.Party.Person.IsDeleted = true;

        if (user.UserGroupMemberships != null)
        {
          user.UserGroupMemberships.ForEach((userGroupMembership) =>
          {
            userGroupMembership.IsDeleted = true;
          });
        }

        if (user.UserAccessRoles != null)
        {
          user.UserAccessRoles.ForEach((userAccessRole) =>
          {
            userAccessRole.IsDeleted = true;
          });
        }

        if (user.UserIdentityProviders != null)
        {
          user.UserIdentityProviders.ForEach((idp) => { idp.IsDeleted = true; });
        }

        if (user.UserType == DbModel.Constants.UserType.Delegation)
        {
          user.DelegationEndDate = DateTime.UtcNow;
        }
      }

      await _dataContext.SaveChangesAsync();

      if (_appConfigInfo.UserRoleApproval.Enable)
      {
        var userAccessRolePendingExpiredList = await _dataContext.UserAccessRolePending.Where(u => !u.IsDeleted && u.UserId == primaryUser.Id).ToListAsync();
        if (userAccessRolePendingExpiredList.Any())
        {
          userAccessRolePendingExpiredList.ForEach(l => { l.IsDeleted = true; l.Status = (int)UserPendingRoleStaus.Removed; });
          await _dataContext.SaveChangesAsync();
        }
      }

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.UserDelete, AuditLogApplication.ManageUserAccount, $"UserId:{primaryUser.Id}");

      // Invalidate redis
      await _cacheInvalidateService.RemoveUserCacheValuesOnDeleteAsync(userName, primaryUser.Party.Person.Organisation.CiiOrganisationId, deletingContactPointIds);
      await _remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + userName, true);

      // Notify the adapter
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Delete, userName, primaryUser.Party.Person.Organisation.CiiOrganisationId);

      try
      {
        await _idamService.DeleteUserInIdamAsync(userName);
      }
      catch (Exception ex)
      {
        // No need to expose the IDAM user deletion error if the user is already deleted from DB
        // Log this
      }
    }

    public async Task VerifyUserAccountAsync(string userName)
    {
      _userHelper.ValidateUserName(userName);
      var user = await _dataContext.User.FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);
      if (user == null)
      {
        throw new ResourceNotFoundException();
      }
      user.AccountVerified = true;
      await _dataContext.SaveChangesAsync();
    }

    public async Task<UserEditResponseInfo> UpdateUserAsync(string userName, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      var isRegisteredInIdam = false;
      _userHelper.ValidateUserName(userName);

      if (userName != userProfileRequestInfo.UserName)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }

      var organisation = await _dataContext.Organisation
        .Include(o => o.UserGroups).ThenInclude(ug => ug.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
        .Include(o => o.OrganisationEligibleIdentityProviders)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .Include(u => u.UserGroupMemberships)
        .Include(u => u.UserAccessRoles)
        .Include(u => u.UserIdentityProviders).ThenInclude(uidp => uidp.OrganisationEligibleIdentityProvider).ThenInclude(oidp => oidp.IdentityProvider)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName && u.UserType == DbModel.Constants.UserType.Primary);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      bool isMyProfile = _requestContext.UserId == user.Id;

      Validate(userProfileRequestInfo, isMyProfile, organisation);
      bool mfaFlagChanged = user.MfaEnabled != userProfileRequestInfo.MfaEnabled;

      var UserAccessRole = (from u in _dataContext.User
                            join ua in _dataContext.UserAccessRole on u.Id equals ua.UserId
                            join er in _dataContext.OrganisationEligibleRole on ua.OrganisationEligibleRoleId equals er.Id
                            join cr in _dataContext.CcsAccessRole on er.CcsAccessRoleId equals cr.Id
                            where (u.UserName == userName && cr.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)
                            select new { er.CcsAccessRole.CcsAccessRoleNameKey }).FirstOrDefault();

      bool isAdminUser = false;
      if (UserAccessRole != null && UserAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)
      {
        isAdminUser = true;
      }

      bool hasProfileInfoChanged;
      if (userProfileRequestInfo.Detail.IdentityProviderIds is not null)
      {
        hasProfileInfoChanged = (user.Party.Person.FirstName != userProfileRequestInfo.FirstName.Trim() ||
                                user.Party.Person.LastName != userProfileRequestInfo.LastName.Trim() ||
                                user.UserTitle != (int)Enum.Parse(typeof(UserTitle), string.IsNullOrWhiteSpace(userProfileRequestInfo.Title) ? "Unspecified" : userProfileRequestInfo.Title) ||
                                user.UserIdentityProviders.Select(uidp => uidp.OrganisationEligibleIdentityProviderId).OrderBy(id => id) != userProfileRequestInfo.Detail.IdentityProviderIds.OrderBy(id => id));
      }
      else
      {
        hasProfileInfoChanged = (user.Party.Person.FirstName != userProfileRequestInfo.FirstName.Trim() ||
                                user.Party.Person.LastName != userProfileRequestInfo.LastName.Trim() ||
                                user.UserTitle != (int)Enum.Parse(typeof(UserTitle), string.IsNullOrWhiteSpace(userProfileRequestInfo.Title) ? "Unspecified" : userProfileRequestInfo.Title));
      }
      // #Delegated If first name or last name updated for primary account update in delegated as well.
      if (user.Party.Person.FirstName != userProfileRequestInfo.FirstName.Trim() || user.Party.Person.LastName != userProfileRequestInfo.LastName.Trim())
      {
        var delegatedOrgDetails = await _dataContext.User.Include(u => u.Party).ThenInclude(p => p.Person).Where(u => u.UserName == userName &&
                                  u.UserType == DbModel.Constants.UserType.Delegation && !u.IsDeleted && u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date).ToListAsync();

        foreach (var delegatedUserDetail in delegatedOrgDetails)
        {
          delegatedUserDetail.Party.Person.FirstName = userProfileRequestInfo.FirstName.Trim();
          delegatedUserDetail.Party.Person.LastName = userProfileRequestInfo.LastName.Trim();
        }
      }

      user.Party.Person.FirstName = userProfileRequestInfo.FirstName.Trim();
      user.Party.Person.LastName = userProfileRequestInfo.LastName.Trim();

      bool hasGroupMembershipsNotChanged = true;
      bool hasRolesNotChanged = true;
      bool hasIdpChange = false;
      List<int> previousGroups = new();
      List<int> previousRoles = new();
      List<int> requestGroups = new();
      List<int> requestRoles = new();
      List<int> previousIdentityProviderIds = new();
      var userAccessRoleRequiredApproval = new List<int>();

      if (!isMyProfile || isAdminUser == true)
      {
        user.UserTitle = (int)Enum.Parse(typeof(UserTitle), string.IsNullOrWhiteSpace(userProfileRequestInfo.Title) ? "Unspecified" : userProfileRequestInfo.Title);
        requestGroups = userProfileRequestInfo.Detail.GroupIds == null ? new List<int>() : userProfileRequestInfo.Detail.GroupIds.OrderBy(e => e).ToList();
        requestRoles = userProfileRequestInfo.Detail.RoleIds == null ? new List<int>() : userProfileRequestInfo.Detail.RoleIds.OrderBy(e => e).ToList();
        hasGroupMembershipsNotChanged = Enumerable.SequenceEqual(requestGroups, user.UserGroupMemberships.Select(ug => ug.OrganisationUserGroup.Id).OrderBy(e => e));
        hasRolesNotChanged = Enumerable.SequenceEqual(requestRoles, user.UserAccessRoles.Select(ur => ur.OrganisationEligibleRoleId).OrderBy(e => e));
        previousGroups = user.UserGroupMemberships.Select(ug => ug.OrganisationUserGroup.Id).ToList();
        previousRoles = user.UserAccessRoles.Select(ur => ur.OrganisationEligibleRoleId).ToList();
        user.UserGroupMemberships.RemoveAll(g => true);
        user.UserAccessRoles.RemoveAll(r => true);

        var isPreviouslyUserNamePwdConnectionIncluded = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
        var isPreviouslyNonUserNamePwdConnectionIncluded = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName);
        var isUserNamePwdConnectionIncluded = true;
        // var isNonUserNamePwdConnectionIncluded = false;
        if (userProfileRequestInfo.Detail.IdentityProviderIds is not null)
        {
          var elegibleIdentityProviders = await _dataContext.OrganisationEligibleIdentityProvider
                                        .Include(x => x.IdentityProvider)
                                        .Where(o => o.Organisation.Id == organisation.Id)
                                        .ToListAsync();

          isUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => elegibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName));
          // isNonUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => elegibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName));
        }
        //if (userProfileRequestInfo.MfaEnabled && isNonUserNamePwdConnectionIncluded)
        //{
        //  throw new CcsSsoException(ErrorConstant.ErrorMfaFlagForInvalidConnection);
        //}

        //mfa flag has removed
        if (!userProfileRequestInfo.MfaEnabled && isUserNamePwdConnectionIncluded)
        {
          //check for any admin role/groups
          var mfaEnabledRoleExists = organisation.OrganisationEligibleRoles.Any(r => userProfileRequestInfo.Detail.RoleIds != null && userProfileRequestInfo.Detail.RoleIds.Any(role => role == r.Id)
                                    && r.MfaEnabled && !r.IsDeleted);

          if (mfaEnabledRoleExists)
          {
            throw new CcsSsoException(ErrorConstant.ErrorMfaFlagRequired);
          }
          else if (userProfileRequestInfo.Detail.GroupIds != null && userProfileRequestInfo.Detail.GroupIds.Any())
          {
            var mfaEnabled = organisation.UserGroups.Any(oug => userProfileRequestInfo.Detail.GroupIds.Contains(oug.Id) && !oug.IsDeleted &&
                              oug.GroupEligibleRoles.Any(er => !er.IsDeleted && er.OrganisationEligibleRole.MfaEnabled));
            if (mfaEnabled)
            {
              throw new CcsSsoException(ErrorConstant.ErrorMfaFlagRequired);
            }
          }
        }

        user.MfaEnabled = userProfileRequestInfo.MfaEnabled;
        if (mfaFlagChanged)
        {
          hasProfileInfoChanged = true;
        }

        // Set groups
        var userGroupMemberships = new List<UserGroupMembership>();
        userProfileRequestInfo.Detail.GroupIds?.ForEach((groupId) =>
        {
          userGroupMemberships.Add(new UserGroupMembership
          {
            OrganisationUserGroupId = groupId
          });
        });
        user.UserGroupMemberships = userGroupMemberships;

        // Set user roles
        var userAccessRoles = new List<UserAccessRole>();

        var ccsAccessRoleRequiredApproval = await _dataContext.CcsAccessRole.Where(x => x.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired).ToListAsync();

        userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
        {
          var ccsAccessRoleId = organisation.OrganisationEligibleRoles.FirstOrDefault(x => x.Id == roleId)?.CcsAccessRoleId;
          var isRoleRequiredApproval = ccsAccessRoleId != null && ccsAccessRoleRequiredApproval != null && ccsAccessRoleRequiredApproval.Any(x => x.Id == ccsAccessRoleId);
          var isUserDomainValid = userName?.ToLower().Split('@')?[1] == organisation.DomainName?.ToLower();

          if (_appConfigInfo.UserRoleApproval.Enable && !isUserDomainValid && isRoleRequiredApproval && !previousRoles.Any(x => x == roleId))
          {
            userAccessRoleRequiredApproval.Add(roleId);
          }
          else
          {
            userAccessRoles.Add(new UserAccessRole
            {
              OrganisationEligibleRoleId = roleId
            });
          }
        });
        user.UserAccessRoles = userAccessRoles;

        // Check the admin group availability in request
        var noAdminRoleGroupInRequest = userProfileRequestInfo.Detail.GroupIds == null || !userProfileRequestInfo.Detail.GroupIds.Any() ||
          !organisation.UserGroups.Any(g => !g.IsDeleted
         && userProfileRequestInfo.Detail.GroupIds.Contains(g.Id)
         && g.GroupEligibleRoles.Any(gr => gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey));

        // Check the admin role availability in request
        var noAdminRoleInRequest = userProfileRequestInfo.Detail.RoleIds == null || !userProfileRequestInfo.Detail.RoleIds.Any() ||
          !organisation.OrganisationEligibleRoles.Any(or => !or.IsDeleted
            && userProfileRequestInfo.Detail.RoleIds.Contains(or.Id)
            && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey);

        if (noAdminRoleGroupInRequest && noAdminRoleInRequest && await IsOrganisationOnlyAdminAsync(user, userName))
        {
          throw new CcsSsoException(ErrorConstant.ErrorCannotRemoveAdminRoleGroupLastOrgAdmin);
        }

        var defaultUserRoleId = organisation.OrganisationEligibleRoles.First(or => or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.DefaultUserRoleNameKey).Id;

        // Set default user role if no role available
        if (userProfileRequestInfo.Detail.RoleIds == null || !userProfileRequestInfo.Detail.RoleIds.Any() || !userAccessRoles.Exists(ur => ur.OrganisationEligibleRoleId == defaultUserRoleId))
        {
          userAccessRoles.Add(new UserAccessRole
          {
            OrganisationEligibleRoleId = defaultUserRoleId
          });
        }

        if (userProfileRequestInfo.Detail.IdentityProviderIds is not null)
        {
          hasIdpChange = !user.UserIdentityProviders
          .Select(uidp => uidp.OrganisationEligibleIdentityProviderId).OrderBy(id => id).SequenceEqual(userProfileRequestInfo.Detail.IdentityProviderIds.OrderBy(id => id));

          previousIdentityProviderIds = user.UserIdentityProviders.Select(uidp => uidp.OrganisationEligibleIdentityProviderId).ToList();

          // Remove idps
          user.UserIdentityProviders.Where(uidp => !uidp.IsDeleted).ToList().ForEach((uidp) =>
          {
            if (!userProfileRequestInfo.Detail.IdentityProviderIds.Contains(uidp.OrganisationEligibleIdentityProviderId))
            {
              uidp.IsDeleted = true;
            }
          });

          // Add new idps
          List<UserIdentityProvider> newUserIdentityProviderList = new();
          userProfileRequestInfo.Detail.IdentityProviderIds.ForEach((id) =>
          {
            if (!user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProviderId == id))
            {
              newUserIdentityProviderList.Add(new UserIdentityProvider { UserId = user.Id, OrganisationEligibleIdentityProviderId = id });
            }
          });
          user.UserIdentityProviders.AddRange(newUserIdentityProviderList);
        }

        if (isPreviouslyUserNamePwdConnectionIncluded && !isUserNamePwdConnectionIncluded) // Conclave connection removed
        {
          await _idamService.DeleteUserInIdamAsync(userName);
        }
        else if (!isPreviouslyUserNamePwdConnectionIncluded && isUserNamePwdConnectionIncluded)  // Conclave connection added
        {
          SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
          {
            Email = userName,
            UserName = userName,
            FirstName = userProfileRequestInfo.FirstName,
            LastName = userProfileRequestInfo.LastName,
            MfaEnabled = user.MfaEnabled,
            SendUserRegistrationEmail = userProfileRequestInfo.SendUserRegistrationEmail
          };

          await _idamService.RegisterUserInIdamAsync(securityApiUserInfo);
          isRegisteredInIdam = true;
        }
        else if (mfaFlagChanged)
        {
          SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
          {
            Email = userName,
            MfaEnabled = user.MfaEnabled
          };

          await _idamService.UpdateUserMfaInIdamAsync(securityApiUserInfo);
        }
      }

      await _dataContext.SaveChangesAsync();

      if (_appConfigInfo.UserRoleApproval.Enable)
      {
        await CreatePendingRoleRequest(userAccessRoleRequiredApproval, user.Id, userName, organisation.CiiOrganisationId);
      }

      // Log
      if (!isMyProfile || isAdminUser == true)
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.UserUpdate, AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}");
        if (hasIdpChange)
        {
          await _remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + userName, true);
          await _auditLoginService.CreateLogAsync(AuditLogEvent.UserIdpUpdate, AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}, NewIdpIds:{string.Join(",", userProfileRequestInfo.Detail.IdentityProviderIds)}, PreviousIdpIds:{string.Join(",", previousIdentityProviderIds)}");
        }
        if (!hasGroupMembershipsNotChanged)
        {
          await _auditLoginService.CreateLogAsync(AuditLogEvent.UserGroupUpdate, AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}, NewGroupIds:{string.Join(",", requestGroups)}, PreviousGroupIds:{string.Join(",", previousGroups)}");
        }
        if (!hasRolesNotChanged)
        {
          await _auditLoginService.CreateLogAsync(AuditLogEvent.UserRoleUpdate, AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}, NewRoleIds:{string.Join(",", requestRoles)}, PreviousRoleIds:{string.Join(",", previousRoles)}");
        }
      }
      else
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.MyAccountUpdate, AuditLogApplication.ManageMyAccount, $"UserId:{user.Id}");
      }

      //Invalidate redis
      var invalidatingCacheKeyList = new List<string>
      {
        $"{CacheKeyConstant.User}-{userName}",
        $"{CacheKeyConstant.OrganisationUsers}-{user.Party.Person.Organisation.CiiOrganisationId}"
      };
      await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeyList.ToArray());

      // Notify the adapter
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Update, userName, organisation.CiiOrganisationId);

      if (!hasGroupMembershipsNotChanged || !hasRolesNotChanged)
      {
        await _ccsSsoEmailService.SendUserPermissionUpdateEmailAsync(userName);
      }

      if (hasProfileInfoChanged)
      {
        await _ccsSsoEmailService.SendUserProfileUpdateEmailAsync(userName);
      }

      return new UserEditResponseInfo
      {
        UserId = userName,
        IsRegisteredInIdam = isRegisteredInIdam
      };
    }

    public async Task ResetUserPasswodAsync(string userName, string? component)
    {
      _userHelper.ValidateUserName(userName);

      userName = userName.ToLower().Trim();

      var user = await _dataContext.User
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      await _idamService.ResetUserPasswordAsync(userName);

      await _remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + userName, true);

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.AdminResetPassword, component == AuditLogApplication.OrgUserSupport ?
        AuditLogApplication.OrgUserSupport : AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}");
    }

    public async Task RemoveAdminRolesAsync(string userName)
    {
      _userHelper.ValidateUserName(userName);

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
        .Include(u => u.UserAccessRoles).ThenInclude(uar => uar.OrganisationEligibleRole).ThenInclude(oer => oer.CcsAccessRole)
        .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup).ThenInclude(ug => ug.GroupEligibleRoles)
          .ThenInclude(ger => ger.OrganisationEligibleRole).ThenInclude(oer => oer.CcsAccessRole)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      if (await IsOrganisationOnlyAdminAsync(user, userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorCannotRemoveAdminRoleGroupLastOrgAdmin);
      }

      // Remove the admin access role from the user
      if (user.UserAccessRoles != null)
      {
        var adminAccessRole = user.UserAccessRoles.FirstOrDefault(uar => !uar.IsDeleted && uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");

        if (adminAccessRole != null)
        {
          adminAccessRole.IsDeleted = true;
        }
      }

      // Remove any group having admin access role from the user
      if (user.UserGroupMemberships != null)
      {
        user.UserGroupMemberships.ForEach((ugm) =>
        {
          var groupRoles = ugm.OrganisationUserGroup.GroupEligibleRoles;
          if (!ugm.IsDeleted && groupRoles != null)
          {
            var adminRole = groupRoles.Any(gr => !gr.IsDeleted && gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");

            if (adminRole)
            {
              ugm.IsDeleted = true;
            }
          }
        });
      }

      await _dataContext.SaveChangesAsync();

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.RemoveUserAdminRoles, AuditLogApplication.OrgUserSupport, $"UserId:{user.Id}");

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.User}-{userName}");

      // Notify the adapter
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Update, userName, user.Party.Person.Organisation.CiiOrganisationId);

      // Send permission upadate email
      await _ccsSsoEmailService.SendUserPermissionUpdateEmailAsync(userName);
    }

    public async Task AddAdminRoleAsync(string userName)
    {
      _userHelper.ValidateUserName(userName);

      var user = await _dataContext.User
        .Include(u => u.UserIdentityProviders)
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
        .Include(u => u.UserAccessRoles)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      var orgId = user.Party.Person.OrganisationId;

      var organisationAdminAccessRole = await _dataContext.OrganisationEligibleRole
        .FirstOrDefaultAsync(oer => !oer.IsDeleted && oer.OrganisationId == user.Party.Person.OrganisationId && oer.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey);

      if (organisationAdminAccessRole == null)
      {
        throw new CcsSsoException("NO_ADMIN_ROLE_FOR_ORGANISATION");
      }

      var userNamePasswordConnection = await _dataContext.OrganisationEligibleIdentityProvider.FirstOrDefaultAsync(oidp => oidp.OrganisationId == orgId &&
                                         oidp.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
      var hasConclaveConnection = user.UserIdentityProviders.Any(uip => uip.OrganisationEligibleIdentityProviderId == userNamePasswordConnection.Id && !uip.IsDeleted);

      var mfaConfiguredInDBForUser = user.MfaEnabled;

      user.MfaEnabled = true;

      if (!hasConclaveConnection)
      {
        user.UserIdentityProviders.Add(new UserIdentityProvider()
        {
          OrganisationEligibleIdentityProviderId = userNamePasswordConnection.Id,
          UserId = user.Id
        });
      }

      //Admins should only have username-password option
      var nonConclaveConnections = user.UserIdentityProviders.Where(uip => uip.OrganisationEligibleIdentityProviderId != userNamePasswordConnection.Id && !uip.IsDeleted).ToList();
      nonConclaveConnections.ForEach(c => c.IsDeleted = true);

      user.UserAccessRoles.Add(new UserAccessRole
      {
        UserId = user.Id,
        OrganisationEligibleRoleId = organisationAdminAccessRole.Id
      });

      await _dataContext.SaveChangesAsync();

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.AddUserAdminRole, AuditLogApplication.OrgUserSupport, $"UserId:{user.Id}");

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.User}-{userName}");

      // Notify the adapter
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Update, userName, user.Party.Person.Organisation.CiiOrganisationId);

      // Send permission upadate email
      await _ccsSsoEmailService.SendUserPermissionUpdateEmailAsync(userName);

      if (!hasConclaveConnection)
      {
        SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
        {
          FirstName = user.Party.Person.FirstName,
          LastName = user.Party.Person.LastName,
          Email = userName,
          MfaEnabled = true,
          SendUserRegistrationEmail = true
        };
        await _idamService.RegisterUserInIdamAsync(securityApiUserInfo);
      }
      else if (!mfaConfiguredInDBForUser)
      {
        SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
        {
          Email = userName,
          MfaEnabled = true,
          SendUserRegistrationEmail = false
        };
        await _idamService.UpdateUserMfaInIdamAsync(securityApiUserInfo);
      }
    }

    public async Task<bool> IsUserExist(string userName)
    {
      var user = await _dataContext.User.FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName.ToLower() == userName.ToLower() && u.UserType == UserType.Primary);

      return user != null;
    }

    private async Task<bool> IsOrganisationOnlyAdminAsync(User user, string userName)
    {
      int organisationId = user.Party.Person.OrganisationId;

      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
        .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;

      // Check any admin role user available for org other than this user
      var anyAdminRoleExists = await _dataContext.User
        .Include(u => u.UserAccessRoles)
        .AnyAsync(u => !u.IsDeleted &&
        u.Party.Person.OrganisationId == organisationId && u.UserName != userName &&
        u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId));

      if (anyAdminRoleExists)
      {
        // If available return this is not the last admin user
        return false;
      }

      else
      {
        // Check any admin group (having a admin role in group) user available for org other than this user
        var anyAdminGroupExists = await _dataContext.User
        .Include(u => u.UserGroupMemberships).ThenInclude(ug => ug.OrganisationUserGroup).ThenInclude(og => og.GroupEligibleRoles)
        .AnyAsync(u => !u.IsDeleted &&
        u.Party.Person.OrganisationId == organisationId && u.UserName != userName &&
        u.UserGroupMemberships.Any(ugm => !ugm.IsDeleted && ugm.OrganisationUserGroup.GroupEligibleRoles.Any(ga => !ga.IsDeleted
          && ga.OrganisationEligibleRoleId == orgAdminAccessRoleId)));

        return !anyAdminGroupExists;
      }
    }

    private void Validate(UserProfileEditRequestInfo userProfileReqestInfo, bool isMyProfile, Organisation organisation)
    {
      if (!UtilityHelper.IsUserNameValid(userProfileReqestInfo.FirstName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidFirstName);
      }

      if (!UtilityHelper.IsUserNameLengthValid(userProfileReqestInfo.FirstName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidFirstNamelength);
      }

      if (!UtilityHelper.IsUserNameValid(userProfileReqestInfo.LastName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidLastName);
      }

      if (!UtilityHelper.IsUserNameLengthValid(userProfileReqestInfo.LastName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidLastNamelength);
      }

      if (userProfileReqestInfo.Detail == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDetail);
      }

      if (!isMyProfile)
      {
        var orgGroupIds = organisation.UserGroups.Select(g => g.Id).ToList();
        var orgRoleIds = organisation.OrganisationEligibleRoles.Select(r => r.Id);
        var orgIdpIds = organisation.OrganisationEligibleIdentityProviders.Select(i => i.Id);
        UserTitle enumTitle;
        if (userProfileReqestInfo.Title != null && !UserTitle.TryParse(userProfileReqestInfo.Title, out enumTitle))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidTitle);
        }

        if (userProfileReqestInfo.Detail.GroupIds != null && userProfileReqestInfo.Detail.GroupIds.Any(gId => !orgGroupIds.Contains(gId)))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserGroup);
        }

        if (userProfileReqestInfo.Detail.RoleIds != null)
        {
          var duplicatesRoleIds = userProfileReqestInfo.Detail.RoleIds.GroupBy(x => x).SelectMany(g => g.Skip(1));
          if (duplicatesRoleIds.Any())
          {
            throw new CcsSsoException(ErrorConstant.ErrorInvalidUserRole);
          }
        }

        if (userProfileReqestInfo.Detail.RoleIds != null && userProfileReqestInfo.Detail.RoleIds.Any(gId => !orgRoleIds.Contains(gId)))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserRole);
        }        

        if (userProfileReqestInfo.Detail.IdentityProviderIds == null || !userProfileReqestInfo.Detail.IdentityProviderIds.Any() ||
          userProfileReqestInfo.Detail.IdentityProviderIds.Any(id => !orgIdpIds.Contains(id)))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidIdentityProvider);
        }
      }
    }

    #region Delegated user

    /// Insert delegated user (Other org user) to represent org 

    public async Task CreateDelegatedUserV1Async(DelegatedUserProfileServiceRoleGroupRequestInfo userProfileRoleGroupRequestInfo)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }
      var userProfileRequestInfo = await ConvertServiceRoleGroupTouserProfileRequest(userProfileRoleGroupRequestInfo);

      await CreateDelegatedUserAsync(userProfileRequestInfo);
    }

    public async Task UpdateDelegatedUserV1Async(DelegatedUserProfileServiceRoleGroupRequestInfo userProfileRoleGroupRequestInfo)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }
      var userProfileRequestInfo = await ConvertServiceRoleGroupTouserProfileRequest(userProfileRoleGroupRequestInfo);

      await UpdateDelegatedUserAsync(userProfileRequestInfo);
    }


    public async Task CreateDelegatedUserAsync(DelegatedUserProfileRequestInfo userProfileRequestInfo)
    {
      var userName = userProfileRequestInfo.UserName.ToLower();
      _userHelper.ValidateUserName(userName);

      if (string.IsNullOrWhiteSpace(userProfileRequestInfo.Detail.DelegatedOrgId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }

      var organisation = (await _dataContext.Organisation.Include(o => o.OrganisationEligibleRoles).ThenInclude(c => c.CcsAccessRole)
                          .FirstOrDefaultAsync(o => !o.IsDeleted &&
                          o.CiiOrganisationId == userProfileRequestInfo.Detail.DelegatedOrgId));

      if (organisation == default)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      ValidateDelegateUserDetails(organisation, userProfileRequestInfo);

      // this includes primary and all delegated accounts
      var existingUserDetails = _dataContext.User.Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
                              .Where(u => u.UserName == userProfileRequestInfo.UserName.Trim() && !u.IsDeleted).ToList();
      // Only allow delegation for verified users only
      var existingUserPrimaryDetails = existingUserDetails.FirstOrDefault(u => u.UserType == DbModel.Constants.UserType.Primary && u.AccountVerified);
      var existingUserDelegatedDetails = existingUserDetails.Where(u => u.UserType == DbModel.Constants.UserType.Delegation && !u.IsDeleted && u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date).ToList();

      if (existingUserPrimaryDetails == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegationPrimaryDetails);
      }

      // User already delegated in org
      if (existingUserDelegatedDetails.Any(u => u.Party.Person.OrganisationId == organisation.Id))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegation);
      }

      // Don't allow to delegate user for same org.
      if (existingUserPrimaryDetails.Party.Person.Organisation.CiiOrganisationId == userProfileRequestInfo.Detail.DelegatedOrgId)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegationSameOrg);
      }

      // Set user roles
      var userAccessRoles = new List<UserAccessRole>();
      userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
      {
        userAccessRoles.Add(new UserAccessRole
        {
          OrganisationEligibleRoleId = roleId
        });
      });

      var partyTypeId = (await _dataContext.PartyType.FirstOrDefaultAsync(p => p.PartyTypeName == PartyTypeName.User)).Id;

      var party = new Party
      {
        PartyTypeId = partyTypeId,
        Person = new Person
        {
          FirstName = existingUserPrimaryDetails.Party.Person.FirstName,
          LastName = existingUserPrimaryDetails.Party.Person.LastName,
          OrganisationId = organisation.Id
        },
        User = new User
        {
          UserName = existingUserPrimaryDetails.UserName,
          UserTitle = existingUserPrimaryDetails.UserTitle,
          AccountVerified = existingUserPrimaryDetails.AccountVerified,
          //UserGroupMemberships = userGroupMemberships,
          UserAccessRoles = userAccessRoles,
          //UserIdentityProviders = userProfileRequestInfo.Detail.IdentityProviderIds.Select(idpId => new UserIdentityProvider
          //{
          //    OrganisationEligibleIdentityProviderId = idpId
          //}).ToList(),
          MfaEnabled = existingUserPrimaryDetails.MfaEnabled,
          CcsServiceId = existingUserPrimaryDetails.CcsServiceId,
          DelegationStartDate = userProfileRequestInfo.Detail.StartDate,
          DelegationEndDate = userProfileRequestInfo.Detail.EndDate,
          UserType = DbModel.Constants.UserType.Delegation,
          OriginOrganizationId = existingUserPrimaryDetails.Party.Person.Organisation.Id
        }
      };


      try
      {
        _dataContext.Party.Add(party);

        await _dataContext.SaveChangesAsync();

        // Send delegation activation email
        await SendUserDelegatedAccessEmailAsync(existingUserPrimaryDetails.UserName, organisation.CiiOrganisationId, organisation.LegalName);

        // Log
        //await _auditLoginService.CreateLogAsync(AuditLogEvent.UserDelegated, AuditLogApplication.ManageUserAccount, $"UserId:{existingUserPrimaryDetails.Id}," + " " +
        //          $"UserRoleIds:{string.Join(",", userAccessRoles.Select(r => r.OrganisationEligibleRoleId))}");
      }
      catch (Exception ex)
      {
        Console.Write(ex);
      }
      //Invalidate redis
      //await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrganisationUsers}-{organisation.CiiOrganisationId}");

      // Notify the adapter
      //await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Create, userProfileRequestInfo.UserName, organisation.CiiOrganisationId);


    }

    /// Update delegated user details
    public async Task UpdateDelegatedUserAsync(DelegatedUserProfileRequestInfo userProfileRequestInfo)
    {
      _userHelper.ValidateUserName(userProfileRequestInfo.UserName);

      if (string.IsNullOrWhiteSpace(userProfileRequestInfo.Detail.DelegatedOrgId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }

      // get organisation actual id from cii organisation id
      var organisation = (await _dataContext.Organisation.Include(o => o.OrganisationEligibleRoles).ThenInclude(c => c.CcsAccessRole)
                          .FirstOrDefaultAsync(o => !o.IsDeleted &&
                          o.CiiOrganisationId == userProfileRequestInfo.Detail.DelegatedOrgId));

      ValidateDelegateUserDetails(organisation, userProfileRequestInfo, true);

      var existingDelegatedUserDetails = await _dataContext.User.Include(u => u.UserAccessRoles)
                                          .Include(u => u.Party).ThenInclude(p => p.Person)
                                          .FirstOrDefaultAsync(u => u.UserName == userProfileRequestInfo.UserName.Trim() &&
                                          !u.IsDeleted &&
                                          u.UserType == DbModel.Constants.UserType.Delegation && u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date &&
                                          u.Party.Person.OrganisationId == organisation.Id);

      if (existingDelegatedUserDetails == default)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegation);
      }

      List<int> requestRoles = userProfileRequestInfo.Detail.RoleIds.OrderBy(e => e).ToList();
      var hasRolesNotChanged = Enumerable.SequenceEqual(requestRoles, existingDelegatedUserDetails.UserAccessRoles.
                               Select(ur => ur.OrganisationEligibleRoleId).OrderBy(e => e));

      if (!hasRolesNotChanged)
      {
        // Set user roles
        var userAccessRoles = new List<UserAccessRole>();
        userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
        {
          userAccessRoles.Add(new UserAccessRole
          {
            OrganisationEligibleRoleId = roleId
          });
        });
        existingDelegatedUserDetails.UserAccessRoles = userAccessRoles;
      }

      if (!existingDelegatedUserDetails.DelegationEndDate.Value.Date.Equals(userProfileRequestInfo.Detail.EndDate.Date))
      {
        existingDelegatedUserDetails.DelegationEndDate = userProfileRequestInfo.Detail.EndDate;
      }

      try
      {
        await _dataContext.SaveChangesAsync();

        // Send the delegation email
        //await _ccsSsoEmailService.SendUserWelcomeEmailAsync(party.User.UserName, string.Join(",", eligibleIdentityProviders.Select(idp => idp.IdentityProvider.IdpName)));

        // Log
        //await _auditLoginService.CreateLogAsync(AuditLogEvent.UserDelegated, AuditLogApplication.ManageUserAccount, $"UserId:{existingUserPrimaryDetails.Id}," + " " +
        //$"UserRoleIds:{string.Join(",", userAccessRoles.Select(r => r.OrganisationEligibleRoleId))}");                
      }
      catch (Exception ex)
      {
        Console.Write(ex);
      }
      //Invalidate redis
      //await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.OrganisationUsers}-{organisation.CiiOrganisationId}");

      // Notify the adapter
      //await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Create, userProfileRequestInfo.UserName, organisation.CiiOrganisationId);
    }

    // Delete user delegation from org
    public async Task RemoveDelegatedAccessForUserAsync(string userName, string organisationId)
    {
      _userHelper.ValidateUserName(userName);

      if (string.IsNullOrWhiteSpace(organisationId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorOrganisationIdRequired);
      }

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
        .Include(u => u.UserAccessRoles)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName &&
        u.UserType == DbModel.Constants.UserType.Delegation && u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date &&
        u.Party.Person.Organisation.CiiOrganisationId == organisationId);

      if (user == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegation);
      }

      user.IsDeleted = true;
      user.Party.IsDeleted = true;
      user.Party.Person.IsDeleted = true;
      user.DelegationEndDate = DateTime.UtcNow;

      if (user.UserAccessRoles != null)
      {
        user.UserAccessRoles.ForEach((userAccessRole) =>
        {
          userAccessRole.IsDeleted = true;
        });
      }

      try
      {
        await _dataContext.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
      }
      // Log
      //await _auditLoginService.CreateLogAsync(AuditLogEvent.UserDelete, AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}");

      // Invalidate redis
      //await _cacheInvalidateService.RemoveUserCacheValuesOnDeleteAsync(userName, user.Party.Person.Organisation.CiiOrganisationId, deletingContactPointIds);
      //await _remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + userName, true);

      // Notify the adapter
      //await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Delete, userName, user.Party.Person.Organisation.CiiOrganisationId);
    }

    /// Update delegated user acceptance
    public async Task AcceptDelegationAsync(string acceptanceToken)
    {
      acceptanceToken = acceptanceToken?.Replace(" ", "+");
      // Decrept token
      string delegationActivationDetails = _cryptographyService.DecryptString(acceptanceToken, _appConfigInfo.DelegationEmailTokenEncryptionKey);

      if (string.IsNullOrWhiteSpace(delegationActivationDetails))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegation);
      }

      //validate token expiration
      Dictionary<string, string> delegationDetails = delegationActivationDetails.Split('&').Select(value => value.Split('='))
                                                  .ToDictionary(pair => pair[0], pair => pair[1]);
      string userName = delegationDetails["usr"];
      string ciiOrganisationId = delegationDetails["org"];
      DateTime expirationTime = Convert.ToDateTime(delegationDetails["exp"]);

      if (expirationTime < DateTime.UtcNow)
      {
        throw new CcsSsoException(ErrorConstant.ErrorActivationLinkExpired);
      }

      // get organisation actual id from ciiorganisation id
      var organisation = (await _dataContext.Organisation
                          .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId));

      if (organisation == default)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidOrganisationName);
      }

      // check redis cache for latest token, if not exist then expired, not same then also expired
      var latestToken = await _remoteCacheService.GetValueAsync<string>(userName + "-" + organisation.CiiOrganisationId);

      if (latestToken?.Trim() != acceptanceToken?.Trim())
      {
        throw new CcsSsoException(ErrorConstant.ErrorActivationLinkExpired);
      }

      var existingDelegatedUserDetails = await _dataContext.User
                                          .Include(u => u.Party).ThenInclude(p => p.Person)
                                          .FirstOrDefaultAsync(u => u.UserName == userName &&
                                          !u.IsDeleted &&
                                          u.UserType == DbModel.Constants.UserType.Delegation && u.DelegationEndDate.Value.Date >= DateTime.UtcNow.Date &&
                                          u.Party.Person.OrganisationId == organisation.Id);

      if (existingDelegatedUserDetails == default)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDelegation);
      }

      existingDelegatedUserDetails.DelegationAccepted = true;

      try
      {
        //remove redis cache token
        await _remoteCacheService.RemoveAsync(userName + "-" + organisation.CiiOrganisationId);

        await _dataContext.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
      }
    }

    public async Task SendUserDelegatedAccessEmailAsync(string userName, string orgId, string orgName = "")
    {
      _userHelper.ValidateUserName(userName);

      if (string.IsNullOrWhiteSpace(orgId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidCiiOrganisationId);
      }

      if (string.IsNullOrWhiteSpace(orgName))
      {
        var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
        .Where(u => !u.IsDeleted && u.UserName == userName && u.UserType == DbModel.Constants.UserType.Delegation
               && u.Party.Person.Organisation.CiiOrganisationId == orgId
               && !u.DelegationAccepted).SingleOrDefaultAsync();

        if (user == null)
        {
          throw new ResourceNotFoundException();
        }
        orgName = user.Party.Person.Organisation.LegalName;
      }


      string activationInfo = "usr=" + userName + "&org=" + orgId + "&exp=" + DateTime.UtcNow.AddHours(_appConfigInfo.DelegationEmailExpirationHours);
      var encryptedInfo = _cryptographyService.EncryptString(activationInfo, _appConfigInfo.DelegationEmailTokenEncryptionKey);


      if (string.IsNullOrWhiteSpace(encryptedInfo))
      {
        throw new CcsSsoException(ErrorConstant.ErrorSendingActivationLink);
      }
      // add username and token in redish cache with 36 hours expiry, if exist then replace
      await _remoteCacheService.SetValueAsync<string>(userName + "-" + orgId, encryptedInfo,
            new TimeSpan(_appConfigInfo.DelegationEmailExpirationHours, 0, 0));

      // Send the delegation email
      await _ccsSsoEmailService.SendUserDelegatedAccessEmailAsync(userName, orgName, encryptedInfo);
    }


    private void ValidateDelegateUserDetails(Organisation organisation, DelegatedUserProfileRequestInfo userProfileRequestInfo, bool isUpdated = false)
    {
      //validate roles
      var excludeRoleIds = new List<Int32>();
      foreach (var role in _appConfigInfo?.DelegationExcludeRoles)
      {
        var roleToExclude = organisation.OrganisationEligibleRoles.FirstOrDefault(r => r.CcsAccessRole.CcsAccessRoleNameKey == role);
        if (roleToExclude != null)
        {
          excludeRoleIds.Add(roleToExclude.Id);
        }
      }

      var orgElegibleRoleIds = organisation.OrganisationEligibleRoles.Where(r => !r.IsDeleted && !r.CcsAccessRole.IsDeleted).Select(r => r.Id);
      if (!userProfileRequestInfo.Detail.RoleIds.Any() || userProfileRequestInfo.Detail.RoleIds.Any(gId => excludeRoleIds.Contains(gId))
          || userProfileRequestInfo.Detail.RoleIds.Any(gId => !orgElegibleRoleIds.Contains(gId)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserRole);
      }

      // date validations, in update case don't validate start date less then today
      if (userProfileRequestInfo.Detail.StartDate == default || userProfileRequestInfo.Detail.EndDate == default ||
          (isUpdated ? false : userProfileRequestInfo.Detail.StartDate.Date < DateTime.UtcNow.Date) ||
          userProfileRequestInfo.Detail.EndDate.Date < userProfileRequestInfo.Detail.StartDate.Date.AddDays(28) ||
          userProfileRequestInfo.Detail.EndDate.Date > userProfileRequestInfo.Detail.StartDate.Date.AddDays(365) ||
          userProfileRequestInfo.Detail.StartDate.Date > userProfileRequestInfo.Detail.EndDate.Date)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidDetails);
      }
    }
    #endregion

    private async Task CreatePendingRoleRequest(List<int> userAccessRoleRequiredApproval, int userId, string userName, string ciiOrganisationId)
    {
      // remove roles that were pending for approval but now no longer required
      var userAccessRoleRequiredToRemoveFromApproval = await _dataContext.UserAccessRolePending.Where(x => !x.IsDeleted && x.UserId == userId
      && !userAccessRoleRequiredApproval.Contains(x.OrganisationEligibleRoleId) && x.Status == (int)UserPendingRoleStaus.Pending).ToListAsync();

      // get roles that are pending for approval
      var existingPendingRequestRole = await _dataContext.UserAccessRolePending.Where(x => !x.IsDeleted && x.UserId == userId
      && userAccessRoleRequiredApproval.Contains(x.OrganisationEligibleRoleId) && x.Status == (int)UserPendingRoleStaus.Pending).ToListAsync();

      // ignore roles which are still pending for approval
      foreach (var existingPendingRequestToIgnore in existingPendingRequestRole)
      {
        userAccessRoleRequiredApproval.Remove(existingPendingRequestToIgnore.OrganisationEligibleRoleId);
      }

      // Remove pending for approval role that are no longer required (not passed in request)
      if (userAccessRoleRequiredToRemoveFromApproval != null && userAccessRoleRequiredToRemoveFromApproval.Any())
      {
        var roleIds = userAccessRoleRequiredToRemoveFromApproval.Select(x => x.OrganisationEligibleRoleId).ToList();

        await _userProfileRoleApprovalService.RemoveApprovalPendingRolesAsync(userName, string.Join(",", roleIds));
      }

      if (userAccessRoleRequiredApproval.Any())
      {
        await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(new UserProfileEditRequestInfo
        {
          UserName = userName,
          OrganisationId = ciiOrganisationId,
          Detail = new UserRequestDetail
          {
            RoleIds = userAccessRoleRequiredApproval
          }
        });
      }

    }

    #region User Profile Version 1
    public async Task<UserProfileServiceRoleGroupResponseInfo> GetUserV1Async(string userName, bool isDelegated = false, bool isSearchUser = false, string delegatedOrgId = "")
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var userProfileServiceRoleGroupResponseInfo = new UserProfileServiceRoleGroupResponseInfo();

      var userProfileResponseInfo = await this.GetUserAsync(userName, isDelegated, isSearchUser, delegatedOrgId);

      if (userProfileResponseInfo != null)
      {
        userProfileServiceRoleGroupResponseInfo = ConvertUserRoleToServiceRoleGroupResponse(userProfileResponseInfo);

        List<ServiceRoleGroupInfo> serviceRoleGroupInfo = new List<ServiceRoleGroupInfo>();

        var roleIds = userProfileResponseInfo.Detail.RolePermissionInfo.Select(x => x.RoleId).ToList();

        var serviceRoleGroups = await _serviceRoleGroupMapperService.OrgRolesToServiceRoleGroupsAsync(roleIds);

        foreach (var serviceRoleGroup in serviceRoleGroups)
        {
          serviceRoleGroupInfo.Add(new ServiceRoleGroupInfo()
          {
            Id = serviceRoleGroup.Id,
            Name = serviceRoleGroup.Name,
            Key = serviceRoleGroup.Key,
          });
        }

        userProfileServiceRoleGroupResponseInfo.Detail.ServiceRoleGroupInfo = serviceRoleGroupInfo;

        var groupIds = userProfileResponseInfo.Detail.UserGroups.Select(x => x.GroupId).Distinct().ToList();

        List<GroupAccessServiceRoleGroup> groupAccessServiceRoleGroups = new List<GroupAccessServiceRoleGroup>();

        foreach (var groupId in groupIds)
        {
          var groupInfo = await _organisationGroupService.GetServiceRoleGroupAsync(userProfileResponseInfo.OrganisationId, groupId);

          if (groupInfo != null && groupInfo.ServiceRoleGroups != null && groupInfo.ServiceRoleGroups.Count > 0)
          {
            foreach (var serviceRoleGroup in groupInfo.ServiceRoleGroups)
            {
              groupAccessServiceRoleGroups.Add(new GroupAccessServiceRoleGroup()
              {
                GroupId = groupInfo.GroupId,
                Group = groupInfo.GroupName,
                AccessServiceRoleGroupId = serviceRoleGroup.Id,
                AccessServiceRoleGroupName = serviceRoleGroup.Name,
              });
            }
          }
          else
          {
            var userGroup = userProfileResponseInfo.Detail.UserGroups.FirstOrDefault(x => x.GroupId == groupId);

            if (userGroup != null)
            {
              groupAccessServiceRoleGroups.Add(new GroupAccessServiceRoleGroup()
              {
                GroupId = userGroup.GroupId,
                Group = userGroup.Group,
              });
            }
          }
        }

        userProfileServiceRoleGroupResponseInfo.Detail.UserGroups = groupAccessServiceRoleGroups;
      }

      return userProfileServiceRoleGroupResponseInfo;
    }

    public async Task<UserEditResponseInfo> CreateUserV1Async(UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo, bool isNewOrgAdmin = false)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var serviceRoleGroupIds = userProfileServiceRoleGroupEditRequestInfo?.Detail?.ServiceRoleGroupIds;
      var organisationId = userProfileServiceRoleGroupEditRequestInfo?.OrganisationId;

      var userName = userProfileServiceRoleGroupEditRequestInfo.UserName?.ToLower();
      _userHelper.ValidateUserName(userName);

      await ValidateorOanisationId(organisationId);

      UserProfileEditRequestInfo userProfileRequestInfo = await ConvertServiceRoleGroupToUserRoleRequest(userProfileServiceRoleGroupEditRequestInfo, serviceRoleGroupIds, organisationId);

      var userProfileResponseInfo = await this.CreateUserAsync(userProfileRequestInfo, isNewOrgAdmin);

      return userProfileResponseInfo;
    }

    public async Task<UserEditResponseInfo> UpdateUserV1Async(string userName, UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var serviceRoleGroupIds = userProfileServiceRoleGroupEditRequestInfo?.Detail?.ServiceRoleGroupIds;
      var organisationId = userProfileServiceRoleGroupEditRequestInfo?.OrganisationId;

      _userHelper.ValidateUserName(userName);

      if (userName != userProfileServiceRoleGroupEditRequestInfo.UserName)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }

      await ValidateorOanisationId(organisationId);

      UserProfileEditRequestInfo userProfileRequestInfo = await ConvertServiceRoleGroupToUserRoleRequest(userProfileServiceRoleGroupEditRequestInfo, serviceRoleGroupIds, organisationId);

      var userProfileResponseInfo = await this.UpdateUserAsync(userName, userProfileRequestInfo);

      return userProfileResponseInfo;
    }

    private async Task ValidateorOanisationId(string organisationId)
    {
      var organisation = await _dataContext.Organisation
              .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == organisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }
    }

    private async Task<UserProfileEditRequestInfo> ConvertServiceRoleGroupToUserRoleRequest(UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo, List<int> serviceRoleGroupIds, string organisationId)
    {
      var roleIds = new List<int>();

      if (serviceRoleGroupIds != null && serviceRoleGroupIds.Count > 0)
      {
        var serviceRoleGroups = await _dataContext.CcsServiceRoleGroup
        .Where(x => !x.IsDeleted && serviceRoleGroupIds.Contains(x.Id))
        .ToListAsync();

        if (serviceRoleGroups.Count != serviceRoleGroupIds.Count)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidService);
        }

        List<OrganisationEligibleRole> organisationEligibleRoles = await _serviceRoleGroupMapperService.ServiceRoleGroupsToOrgRolesAsync(serviceRoleGroupIds, organisationId);

        var userDomain = userProfileServiceRoleGroupEditRequestInfo?.UserName?.ToLower().Split('@')?[1];
        var orgDoamin = _dataContext.Organisation.FirstOrDefault(o => o.CiiOrganisationId == organisationId)?.DomainName?.ToLower();

        if (userDomain?.Trim() != orgDoamin?.Trim())
        {
          await _serviceRoleGroupMapperService.RemoveApprovalRequiredRoleGroupOtherRolesAsync(organisationEligibleRoles);
        }

        roleIds = organisationEligibleRoles.Select(x => x.Id).ToList();
      }
      else
      {
        roleIds = new List<int>();
      }

      return new UserProfileEditRequestInfo
      {
        UserName = userProfileServiceRoleGroupEditRequestInfo.UserName,
        OrganisationId = userProfileServiceRoleGroupEditRequestInfo.OrganisationId,
        FirstName = userProfileServiceRoleGroupEditRequestInfo.FirstName,
        LastName = userProfileServiceRoleGroupEditRequestInfo.LastName,
        Title = userProfileServiceRoleGroupEditRequestInfo.Title,
        MfaEnabled = userProfileServiceRoleGroupEditRequestInfo.MfaEnabled,
        Password = userProfileServiceRoleGroupEditRequestInfo.Password,
        AccountVerified = userProfileServiceRoleGroupEditRequestInfo.AccountVerified,
        SendUserRegistrationEmail = userProfileServiceRoleGroupEditRequestInfo.SendUserRegistrationEmail,
        OriginOrganisationName = userProfileServiceRoleGroupEditRequestInfo.OriginOrganisationName,
        CompanyHouseId = userProfileServiceRoleGroupEditRequestInfo.CompanyHouseId,
        Detail = new UserRequestDetail
        {
          Id = userProfileServiceRoleGroupEditRequestInfo.Detail.Id,
          GroupIds = userProfileServiceRoleGroupEditRequestInfo.Detail.GroupIds,
          RoleIds = roleIds,
          IdentityProviderIds = userProfileServiceRoleGroupEditRequestInfo.Detail.IdentityProviderIds,
        }
      };
    }

    private static UserProfileServiceRoleGroupResponseInfo ConvertUserRoleToServiceRoleGroupResponse(UserProfileResponseInfo userProfileResponseInfo)
    {
      return new UserProfileServiceRoleGroupResponseInfo
      {
        UserName = userProfileResponseInfo.UserName,
        OrganisationId = userProfileResponseInfo.OrganisationId,
        OriginOrganisationName = userProfileResponseInfo.OriginOrganisationName,
        FirstName = userProfileResponseInfo.FirstName,
        LastName = userProfileResponseInfo.LastName,
        MfaEnabled = userProfileResponseInfo.MfaEnabled,
        Title = userProfileResponseInfo.Title,
        AccountVerified = userProfileResponseInfo.AccountVerified,
        Detail = new UserServiceRoleGroupResponseDetail
        {
          Id = userProfileResponseInfo.Detail.Id,
          CanChangePassword = userProfileResponseInfo.Detail.CanChangePassword,
          IdentityProviders = userProfileResponseInfo.Detail.IdentityProviders,
          DelegatedOrgs = userProfileResponseInfo.Detail.DelegatedOrgs
        }
      };
    }
    private async Task<DelegatedUserProfileRequestInfo> ConvertServiceRoleGroupTouserProfileRequest(DelegatedUserProfileServiceRoleGroupRequestInfo delegatedUserRoleGroupRequestInfo)
    {
      var roleIds = new List<int>();

      var serviceRoleGroupIds = delegatedUserRoleGroupRequestInfo.Detail.ServiceRoleGroupIds;

      if (serviceRoleGroupIds != null && serviceRoleGroupIds.Count > 0)
      {
        var serviceRoleGroups = await _dataContext.CcsServiceRoleGroup
        .Where(x => !x.IsDeleted && serviceRoleGroupIds.Contains(x.Id))
        .ToListAsync();

        if (serviceRoleGroups.Count != serviceRoleGroupIds.Count)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidService);
        }

        List<OrganisationEligibleRole> organisationEligibleRoles = await _serviceRoleGroupMapperService.ServiceRoleGroupsToOrgRolesAsync(serviceRoleGroupIds, delegatedUserRoleGroupRequestInfo.Detail.DelegatedOrgId);

        roleIds = organisationEligibleRoles.Select(x => x.Id).ToList();
      }
      else
      {
        roleIds = new List<int>();
      }

      return new DelegatedUserProfileRequestInfo
      {
        UserName = delegatedUserRoleGroupRequestInfo.UserName,
        Detail = new DelegatedUserRequestDetail
        {
          DelegatedOrgId = delegatedUserRoleGroupRequestInfo.Detail.DelegatedOrgId,
          EndDate = delegatedUserRoleGroupRequestInfo.Detail.EndDate,
          StartDate = delegatedUserRoleGroupRequestInfo.Detail.StartDate,
          RoleIds = roleIds
        }
      };

    }
    #endregion

    public async Task<OrganisationJoinRequest> GetUserJoinRequestDetails(string joiningDetailsToken)
    {
      Dictionary<string, string> orgJoiningDetailList = DecryptTokenAndReturnDetails(joiningDetailsToken);
      string errorCode = await ValidateJoiningRequestAsync(orgJoiningDetailList);

      if (!string.IsNullOrWhiteSpace(errorCode))
      {
        return new OrganisationJoinRequest()
        {
          Email = orgJoiningDetailList["email"].Trim(),
          ErrorCode = errorCode
        };
      }

      return new OrganisationJoinRequest()
      {
        FirstName = orgJoiningDetailList["first"].Trim(),
        LastName = orgJoiningDetailList["last"].Trim(),
        Email = orgJoiningDetailList["email"].Trim(),
        CiiOrgId = orgJoiningDetailList["org"].Trim(),
        ErrorCode = errorCode
      };
    }

    private Dictionary<string, string> DecryptTokenAndReturnDetails(string joiningDetailsToken)
    {
      joiningDetailsToken = joiningDetailsToken?.Replace(" ", "+");

      string orgJoiningDetails = _cryptographyService.DecryptString(joiningDetailsToken, _appConfigInfo.TokenEncryptionKey);

      if (string.IsNullOrWhiteSpace(orgJoiningDetails))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserDetail);
      }

      Dictionary<string, string> orgJoiningDetailList = orgJoiningDetails.Split('&').Select(value => value.Split('='))
                                                  .ToDictionary(pair => pair[0], pair => pair[1]);

      return orgJoiningDetailList;
    }

    private async Task<string> ValidateJoiningRequestAsync(Dictionary<string, string> orgJoiningDetailList)
    {
      string errorCode = string.Empty;
      DateTime expirationTime = Convert.ToDateTime(orgJoiningDetailList["exp"]);

      if (_requestContext.CiiOrganisationId != orgJoiningDetailList["org"]?.Trim())
      {
        throw new ForbiddenException();
      }
      else if (expirationTime < DateTime.UtcNow)
      {
        errorCode = ErrorConstant.ErrorLinkExpired;
      }
      else if (await IsUserExist(orgJoiningDetailList["email"]?.Trim()))
      {
        errorCode = ErrorConstant.ErrorUserAlreadyExists;
      }

      return errorCode;
    }
  }
}