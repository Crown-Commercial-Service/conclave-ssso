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
using CcsSso.Shared.Cache.Contracts;
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
    public UserProfileService(IDataContext dataContext, IUserProfileHelperService userHelper,
      RequestContext requestContext, IIdamService idamService, ICcsSsoEmailService ccsSsoEmailService,
      IAdaptorNotificationService adapterNotificationService, IWrapperCacheService wrapperCacheService,
      IAuditLoginService auditLoginService, IRemoteCacheService remoteCacheService, ICacheInvalidateService cacheInvalidateService)
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
    }

    public async Task<UserEditResponseInfo> CreateUserAsync(UserProfileEditRequestInfo userProfileRequestInfo)
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
        .Where(i => !i.IsDeleted && userProfileRequestInfo.Detail.IdentityProviderIds.Contains(i.Id)).ToListAsync();

      var isConclaveConnectionIncluded = eligibleIdentityProviders.Any(idp => idp.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
      var isNonUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => eligibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName));

      // This is to enforce MFA over any other
      if (userProfileRequestInfo.MfaEnabled && isNonUserNamePwdConnectionIncluded)
      {
        throw new CcsSsoException(ErrorConstant.ErrorMfaFlagForInvalidConnection);
      }

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
      userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
      {
        userAccessRoles.Add(new UserAccessRole
        {
          OrganisationEligibleRoleId = roleId
        });
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


      if (isConclaveConnectionIncluded)
      {
        SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
        {
          Email = userName,
          Password = userProfileRequestInfo.Password,
          SendUserRegistrationEmail = userProfileRequestInfo.SendUserRegistrationEmail,
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
      else if (userProfileRequestInfo.SendUserRegistrationEmail)
      {
        // Send the welcome email if not Idam user. (Idam users will recieve an email while registering)
        await _ccsSsoEmailService.SendUserWelcomeEmailAsync(party.User.UserName, string.Join(",", eligibleIdentityProviders.Select(idp => idp.IdentityProvider.IdpName)));
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

    public async Task<UserProfileResponseInfo> GetUserAsync(string userName)
    {
      _userHelper.ValidateUserName(userName);

      var user = await _dataContext.User
        .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
        .ThenInclude(oug => oug.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)

        .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)

        .Include(u => u.Party).ThenInclude(p => p.Person)
        .ThenInclude(pr => pr.Organisation)
        .Include(u => u.UserIdentityProviders).ThenInclude(uidp => uidp.OrganisationEligibleIdentityProvider).ThenInclude(oi => oi.IdentityProvider)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user != null)
      {
        var userProfileInfo = new UserProfileResponseInfo
        {
          UserName = user.UserName,
          OrganisationId = user.Party.Person.Organisation.CiiOrganisationId,
          FirstName = user.Party.Person.FirstName,
          LastName = user.Party.Person.LastName,
          MfaEnabled = user.MfaEnabled,
          Title = Enum.GetName(typeof(UserTitle), user.UserTitle),
          AccountVerified = user.AccountVerified,
          Detail = new UserResponseDetail
          {
            Id = user.Id,
            CanChangePassword = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName),
            IdentityProviders = user.UserIdentityProviders.Where(uidp => !uidp.IsDeleted).Select(idp => new UserIdentityProviderInfo
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
            }).ToList()
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

    public async Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, string searchString = null, bool includeSelf = false)
    {

      if (!await _dataContext.Organisation.AnyAsync(o => !o.IsDeleted && o.CiiOrganisationId == organisationId))
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

      var userPagedInfo = await _dataContext.GetPagedResultAsync(_dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .Where(u => !u.IsDeleted && (includeSelf || u.Id != _requestContext.UserId) &&
        u.Party.Person.Organisation.CiiOrganisationId == organisationId &&
        (string.IsNullOrWhiteSpace(searchString) || u.UserName.ToLower().Contains(searchString)
        || (havingMultipleWords && u.Party.Person.FirstName.ToLower().Contains(searchFirstNameLowerCase) && u.Party.Person.LastName.ToLower().Contains(searchLastNameLowerCase))
        || (!havingMultipleWords && (u.Party.Person.FirstName.ToLower().Contains(searchString) || u.Party.Person.LastName.ToLower().Contains(searchString)))
        ))
        .OrderBy(u => u.Party.Person.FirstName).ThenBy(u => u.Party.Person.LastName), resultSetCriteria);

      var userListResponse = new UserListResponse
      {
        OrganisationId = organisationId,
        CurrentPage = userPagedInfo.CurrentPage,
        PageCount = userPagedInfo.PageCount,
        RowCount = userPagedInfo.RowCount,
        UserList = userPagedInfo.Results != null ? userPagedInfo.Results.Select(up => new UserListInfo
        {
          Name = $"{up.Party.Person.FirstName} {up.Party.Person.LastName}",
          UserName = up.UserName
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

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
        .Include(u => u.UserGroupMemberships)
        .Include(u => u.UserAccessRoles)
        .Include(u => u.UserIdentityProviders)
        .Include(u => u.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses) // Get virtual addresses
        .Include(u => u.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.ContactPoints) // Assigned contact points
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      if (checkForLastAdmin && await IsOrganisationOnlyAdminAsync(user, userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorCannotDeleteLastOrgAdmin);
      }

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

      List<int> deletingContactPointIds = new();
      if (user.Party.ContactPoints != null)
      {
        user.Party.ContactPoints.ForEach((cp) =>
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

      await _dataContext.SaveChangesAsync();

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.UserDelete, AuditLogApplication.ManageUserAccount, $"UserId:{user.Id}");

      // Invalidate redis
      await _cacheInvalidateService.RemoveUserCacheValuesOnDeleteAsync(userName, user.Party.Person.Organisation.CiiOrganisationId, deletingContactPointIds);
      await _remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + userName, true);

      // Notify the adapter
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Delete, userName, user.Party.Person.Organisation.CiiOrganisationId);

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

    public async Task<UserEditResponseInfo> UpdateUserAsync_old(string userName, UserProfileEditRequestInfo userProfileRequestInfo)
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
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      bool isMyProfile = _requestContext.UserId == user.Id;

      Validate(userProfileRequestInfo, isMyProfile, organisation);
      bool mfaFlagChanged = user.MfaEnabled != userProfileRequestInfo.MfaEnabled;
      bool isAdminUser = userProfileRequestInfo.IsAdminUser;

      //var CcsAccessRole = (from u in _dataContext.User
      //                    join ua in _dataContext.UserAccessRole on u.Id equals ua.UserId
      //                    join er in _dataContext.OrganisationEligibleRole on ua.OrganisationEligibleRoleId equals er.Id
      //                    join cr in _dataContext.CcsAccessRole on er.CcsAccessRoleId equals cr.Id
      //                    where (u.UserName == userName && cr.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)
      //                    select new { er.CcsAccessRole }).FirstOrDefault();

      //bool isAdminUser = true;
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

        var elegibleIdentityProviders = await _dataContext.OrganisationEligibleIdentityProvider
                                        .Include(x => x.IdentityProvider)
                                        .ToListAsync();

        var isPreviouslyUserNamePwdConnectionIncluded = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
        var isPreviouslyNonUserNamePwdConnectionIncluded = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName);
        var isUserNamePwdConnectionIncluded = true;
        var isNonUserNamePwdConnectionIncluded = false;
        if (userProfileRequestInfo.Detail.IdentityProviderIds is not null)
        {
          isUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => elegibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName));
          isNonUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => elegibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName));
        }
        if (userProfileRequestInfo.MfaEnabled && isNonUserNamePwdConnectionIncluded)
        {
          throw new CcsSsoException(ErrorConstant.ErrorMfaFlagForInvalidConnection);
        }

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
        userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
        {
          userAccessRoles.Add(new UserAccessRole
          {
            OrganisationEligibleRoleId = roleId
          });
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
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      bool isMyProfile = _requestContext.UserId == user.Id;

      Validate(userProfileRequestInfo, isMyProfile, organisation);
      bool mfaFlagChanged = user.MfaEnabled != userProfileRequestInfo.MfaEnabled;
      bool isAdminUser = userProfileRequestInfo.IsAdminUser;

      var CcsAccessRole = (from u in _dataContext.User
                           join ua in _dataContext.UserAccessRole on u.Id equals ua.UserId
                           join er in _dataContext.OrganisationEligibleRole on ua.OrganisationEligibleRoleId equals er.Id
                           join cr in _dataContext.CcsAccessRole on er.CcsAccessRoleId equals cr.Id
                           where (u.UserName == userName && cr.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)
                           select new { er.CcsAccessRole }).FirstOrDefault();

      //bool isAdminUser = true;
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

        var elegibleIdentityProviders = await _dataContext.OrganisationEligibleIdentityProvider
                                        .Include(x => x.IdentityProvider)
                                        .ToListAsync();

        var isPreviouslyUserNamePwdConnectionIncluded = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
        var isPreviouslyNonUserNamePwdConnectionIncluded = user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName);
        var isUserNamePwdConnectionIncluded = true;
        var isNonUserNamePwdConnectionIncluded = false;
        if (userProfileRequestInfo.Detail.IdentityProviderIds is not null)
        {
          isUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => elegibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName));
          isNonUserNamePwdConnectionIncluded = userProfileRequestInfo.Detail.IdentityProviderIds.Any(id => elegibleIdentityProviders.Any(oidp => oidp.Id == id && oidp.IdentityProvider.IdpConnectionName != Contstant.ConclaveIdamConnectionName));
        }
        if (userProfileRequestInfo.MfaEnabled && isNonUserNamePwdConnectionIncluded)
        {
          throw new CcsSsoException(ErrorConstant.ErrorMfaFlagForInvalidConnection);
        }

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
        userProfileRequestInfo.Detail.RoleIds?.ForEach((roleId) =>
        {
          userAccessRoles.Add(new UserAccessRole
          {
            OrganisationEligibleRoleId = roleId
          });
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


  }
}
