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
  public class OrganisationGroupService : IOrganisationGroupService
  {
    private readonly IDataContext _dataContext;
    private readonly IUserProfileHelperService _userProfileHelperService;
    private readonly IAuditLoginService _auditLoginService;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly IWrapperCacheService _wrapperCacheService;
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly IServiceRoleGroupMapperService _serviceRoleGroupMapperService;
    private readonly IOrganisationProfileService _organisationService;
    private readonly IUserProfileRoleApprovalService _userProfileRoleApprovalService;
    private readonly ILocalCacheService _localCacheService;
    private readonly RequestContext _requestContext;
    private readonly IExternalHelperService _externalHelperService;
    private readonly IIdamService _idamService;

    public OrganisationGroupService(IDataContext dataContext, IUserProfileHelperService userProfileHelperService,
      IAuditLoginService auditLoginService, ICcsSsoEmailService ccsSsoEmailService, IWrapperCacheService wrapperCacheService,
      ApplicationConfigurationInfo appConfigInfo, IServiceRoleGroupMapperService serviceRoleGroupMapperService,
      IOrganisationProfileService organisationService,
      IUserProfileRoleApprovalService userProfileRoleApprovalService, 
      ILocalCacheService localCacheService, RequestContext requestContext, IExternalHelperService externalHelperService,
      IIdamService idamService)
    {
      _dataContext = dataContext;
      _userProfileHelperService = userProfileHelperService;
      _auditLoginService = auditLoginService;
      _ccsSsoEmailService = ccsSsoEmailService;
      _wrapperCacheService = wrapperCacheService;
      _appConfigInfo = appConfigInfo;
      _serviceRoleGroupMapperService = serviceRoleGroupMapperService;
      _organisationService = organisationService;
      _userProfileRoleApprovalService = userProfileRoleApprovalService;
      _localCacheService = localCacheService;
      _requestContext = requestContext;
      _externalHelperService = externalHelperService;
      _idamService = idamService;
    }

    public async Task<int> CreateGroupAsync(string ciiOrganisationId, OrganisationGroupNameInfo organisationGroupNameInfo)
    {
      //should not allow null and empty space
      if (string.IsNullOrWhiteSpace(organisationGroupNameInfo.GroupName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupName);
      }

      //name must have at least 1 alphanumeric and do not allow all special charactes.
      var IsLetter = organisationGroupNameInfo.GroupName.Any(char.IsLetter);
      var IsNumber = organisationGroupNameInfo.GroupName.Any(char.IsNumber);
      if (IsLetter == false && IsNumber == false)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupName);
      }

      //All other special characters not specified in accepted. min 3 max 256
      if (!UtilityHelper.IsGroupNameValid(organisationGroupNameInfo.GroupName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupName);
      }

      var organisation = await _dataContext.Organisation
       .Include(o => o.UserGroups)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      if (organisation.UserGroups.Any(u => !u.IsDeleted && u.UserGroupName == organisationGroupNameInfo.GroupName))
      {
        throw new ResourceAlreadyExistsException();
      }

      var group = new OrganisationUserGroup
      {
        OrganisationId = organisation.Id,
        UserGroupName = organisationGroupNameInfo.GroupName.Trim(),
        GroupType = organisationGroupNameInfo.GroupType,
        UserGroupNameKey = organisationGroupNameInfo.GroupName.Trim().ToUpper()
      };

      _dataContext.OrganisationUserGroup.Add(group);

      await _dataContext.SaveChangesAsync();

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeCreate, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, GroupName:{group.UserGroupName}, OrganisationId:{ciiOrganisationId}");

      return group.Id;
    }

    public async Task DeleteGroupAsync(string ciiOrganisationId, int groupId)
    {
      var group = await _dataContext.OrganisationUserGroup
        .Include(g => g.GroupEligibleRoles)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User)
        .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && g.Organisation.CiiOrganisationId == ciiOrganisationId);

      if (group == null)
      {
        throw new ResourceNotFoundException();
      }

      if (group.GroupType == (int)GroupType.Admin)
      {
        throw new CcsSsoException(ErrorConstant.ErrorCannotDeleteAdminGroup);
      }
      group.IsDeleted = true;
      group.GroupEligibleRoles.ForEach((groupRoles) => { groupRoles.IsDeleted = true; });
      group.UserGroupMemberships.ForEach((groupMembership) => { groupMembership.IsDeleted = true; });

      await _dataContext.SaveChangesAsync();

      await RemoveGroupRolePendingRequest(group);

      // Log
      await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeDelete, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, GroupName:{group.UserGroupName}, OrganisationId:{ciiOrganisationId}");

      // Invalidate redis
      var invalidatingCacheKeys = new List<string>();
      invalidatingCacheKeys.AddRange(group.UserGroupMemberships.Select(ugm => $"{CacheKeyConstant.User}-{ugm.User.UserName}"));
      await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());
    }

    public async Task<OrganisationGroupResponseInfo> GetGroupAsync(string ciiOrganisationId, int groupId)
    {
      var group = await _dataContext.OrganisationUserGroup
        .Include(g => g.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User).ThenInclude(u => u.UserAccessRoles).ThenInclude(oe => oe.OrganisationEligibleRole).ThenInclude(c => c.CcsAccessRole)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User).ThenInclude(u => u.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && !g.Organisation.IsDeleted && g.Organisation.CiiOrganisationId == ciiOrganisationId);

      if (group == null)
      {
        throw new ResourceNotFoundException();
      }
      var groupadminUsers = await GetGroupAdminUsersAsync(ciiOrganisationId);
      OrganisationGroupResponseInfo organisationGroupResponseInfo = new OrganisationGroupResponseInfo
      {
        GroupId = group.Id,
        MfaEnabled = group.MfaEnabled,
        OrganisationId = ciiOrganisationId,
        GroupName = group.UserGroupName,
        GroupType = group.GroupType,
        CreatedDate = group.CreatedOnUtc.Date.ToString(DateTimeFormat.DateFormatShortMonth),
        Roles = group.GroupEligibleRoles.Where(gr => !gr.IsDeleted).Select(gr => new GroupRole
        {
          Id = gr.OrganisationEligibleRole.Id,
          Name = gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName
        }).ToList()

      };
      var isApprovalRequired = group.GroupEligibleRoles.Any(x => !x.OrganisationEligibleRole.IsDeleted && x.OrganisationEligibleRole.CcsAccessRole.ApprovalRequired == 1);

      organisationGroupResponseInfo.Users = group.UserGroupMemberships.Where(ugm => !ugm.IsDeleted).Select(ugm => new GroupUser
      {
        UserId = ugm.User.UserName,
        Name = $"{ugm.User.Party.Person.FirstName} {ugm.User.Party.Person.LastName}",
        IsAdmin = ugm.User.UserAccessRoles.Any(r => !r.IsDeleted && r.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey && !r.OrganisationEligibleRole.IsDeleted) || groupadminUsers.Any(x => x.Id == ugm.User.Id),

        UserPendingRoleStatus = !isApprovalRequired ? null : getUserRolePendingStatus(ugm),
      }).ToList();

      return organisationGroupResponseInfo;
    }

    private UserPendingRoleStaus? getUserRolePendingStatus(UserGroupMembership ugm)
    {
      // return _dataContext.UserAccessRolePending.OrderByDescending(y => y.Id).FirstOrDefault(x => !x.IsDeleted && x.UserId == ugm.User.Id && x.Status == (int)UserPendingRoleStaus.Pending) != null;
      var pendingRole = _dataContext.UserAccessRolePending
          .OrderByDescending(y => y.Id)
          .FirstOrDefault(x => x.UserId == ugm.User.Id && x.OrganisationUserGroupId == ugm.OrganisationUserGroupId);


      return (UserPendingRoleStaus?)(pendingRole?.Status);
    }

    public async Task<OrganisationGroupList> GetGroupsAsync(string ciiOrganisationId, string searchString = null)
    {
      var organisation = await _dataContext.Organisation
       .Include(o => o.UserGroups).ThenInclude(r => r.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var groups = organisation.UserGroups
        .Where(g => !g.IsDeleted && (string.IsNullOrWhiteSpace(searchString) || g.UserGroupName.ToLower().Contains(searchString.ToLower())))
        .Select(g => new OrganisationGroupInfo
        {
          GroupId = g.Id,
          GroupName = g.UserGroupName,
          GroupType = g.GroupType,
          CreatedDate = g.CreatedOnUtc.Date.ToString(DateTimeFormat.DateFormatShortMonth),
          Roles = g.GroupEligibleRoles.Where(gr => !gr.IsDeleted).Select(gr => new GroupRole
          {
            Id = gr.OrganisationEligibleRole.Id,
            Name = gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
            Description = gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleDescription,
          }).ToList()
        }).OrderByDescending(g => g.GroupType).ThenBy(g => g.GroupName).ToList();

      return new OrganisationGroupList
      {
        OrganisationId = organisation.CiiOrganisationId,
        GroupList = groups
      };
    }
    public async Task<List<User>> GetGroupAdminUsersAsync(string ciiOrganisationId)
    {
      List<User> groupAdminUsers = new List<User>();
      var users = await _dataContext.User
      .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
      .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup).ThenInclude(ug => ug.GroupEligibleRoles)
      .ThenInclude(ger => ger.OrganisationEligibleRole).ThenInclude(oer => oer.CcsAccessRole)
      .Where(u => !u.IsDeleted
           && (string.IsNullOrEmpty(ciiOrganisationId) || u.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId))
      .ToListAsync();

      foreach (var user in users)
      {
        if (user.UserGroupMemberships != null)
        {
          GetAdminUsers(groupAdminUsers, user);
        }
      }
      return groupAdminUsers;
    }

    private static void GetAdminUsers(List<User> groupAdminUsers, User user)
    {
      user.UserGroupMemberships.ForEach((ugm) =>
      {
        var groupRoles = ugm.OrganisationUserGroup.GroupEligibleRoles;
        if (!ugm.IsDeleted && groupRoles != null)
        {
          var isAdminRole = groupRoles.Any(gr => !gr.IsDeleted && gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey);
          if (isAdminRole)
          {
            groupAdminUsers.Add(user);
          }
        }
      });
    }

    public async Task UpdateGroupAsync(string ciiOrganisationId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo)
    {
      var group = await _dataContext.OrganisationUserGroup
        .Include(g => g.GroupEligibleRoles).ThenInclude(r => r.OrganisationEligibleRole).ThenInclude(x => x.CcsAccessRole)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User)
        .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && g.Organisation.CiiOrganisationId == ciiOrganisationId);

      if (group == null)
      {
        throw new ResourceNotFoundException();
      }

      //Add/Update User and Roles 
      if (string.IsNullOrWhiteSpace(organisationGroupRequestInfo.GroupName) && organisationGroupRequestInfo.RoleInfo == null && organisationGroupRequestInfo.UserInfo == null)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupName);
      }

      //All other special characters not specified in accepted. min 3 max 256
      if (organisationGroupRequestInfo.GroupName != null && !UtilityHelper.IsGroupNameValid(organisationGroupRequestInfo.GroupName.Trim()))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupName);
      }

      if (!string.IsNullOrWhiteSpace(organisationGroupRequestInfo.GroupName))
      {
        //name must have at least 1 alphanumeric and do not allow all special charactes.
        var IsLetter = organisationGroupRequestInfo.GroupName.Any(char.IsLetter);
        var IsNumber = organisationGroupRequestInfo.GroupName.Any(char.IsNumber);
        if (IsLetter == false && IsNumber == false)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupName);
        }
      }

      var existingUserNames = group.UserGroupMemberships.Select(ugm => ugm.User.UserName).ToList();
      string newName = string.Empty;
      string previousName = group.UserGroupName;
      bool hasNameChanged = false;
      List<int> addedRoleIds = new();
      List<int> removedRoleIds = new();
      List<Tuple<int, string>> addedUsersTupleList = new();
      List<Tuple<int, string>> removedUsersTupleList = new();
      var mfaEnableInGroup = false;

      if (!string.IsNullOrWhiteSpace(organisationGroupRequestInfo.GroupName))
      {
        await CheckInvalidAdminGroupNameInfo(organisationGroupRequestInfo,group.GroupType);

        if (await _dataContext.OrganisationUserGroup.AnyAsync(oug => !oug.IsDeleted && oug.Organisation.CiiOrganisationId == ciiOrganisationId
          && oug.Id != groupId && oug.UserGroupName == organisationGroupRequestInfo.GroupName))
        {
          throw new ResourceAlreadyExistsException();
        }

        newName = organisationGroupRequestInfo.GroupName.Trim();
        hasNameChanged = newName != previousName;

        group.UserGroupName = newName;
        group.UserGroupNameKey = organisationGroupRequestInfo.GroupName.Trim().ToUpper();
      }

      // Take the not deleted org role ids
      var orgRoleInfo = await _dataContext.OrganisationEligibleRole
        .Where(r => !r.IsDeleted && r.Organisation.CiiOrganisationId == ciiOrganisationId)
        .Select(r => new
        {
          Id = r.Id,
          MfaEnable = r.MfaEnabled
        }).ToListAsync();

      // handle role update
      if (organisationGroupRequestInfo.RoleInfo != null)
      {
        if (organisationGroupRequestInfo.RoleInfo.AddedRoleIds == null && organisationGroupRequestInfo.RoleInfo.RemovedRoleIds == null)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
        }

        if ((organisationGroupRequestInfo.RoleInfo.AddedRoleIds != null &&
          organisationGroupRequestInfo.RoleInfo.AddedRoleIds.Any(id => !orgRoleInfo.Any(r => r.Id == id)))
          || (organisationGroupRequestInfo.RoleInfo.RemovedRoleIds != null &&
          organisationGroupRequestInfo.RoleInfo.RemovedRoleIds.Any(id => !orgRoleInfo.Any(r => r.Id == id))))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
        }
        else
        {
          await CheckInvalidAdminGroupRoleInfo(organisationGroupRequestInfo,group.GroupType);

          var roleIds = organisationGroupRequestInfo.RoleInfo.AddedRoleIds.Concat(organisationGroupRequestInfo.RoleInfo.RemovedRoleIds);
          await CheckInvalidRoleInfo(roleIds.ToList());

          // Remove roles
          if (organisationGroupRequestInfo.RoleInfo.RemovedRoleIds != null)
          {
            removedRoleIds = organisationGroupRequestInfo.RoleInfo.RemovedRoleIds.Distinct().ToList();  // for logs
            group.GroupEligibleRoles.RemoveAll(ga => removedRoleIds.Contains(ga.OrganisationEligibleRoleId));
          }
          // Add roles
          if (organisationGroupRequestInfo.RoleInfo.AddedRoleIds != null)
          {
            addedRoleIds = organisationGroupRequestInfo.RoleInfo.AddedRoleIds.Distinct().ToList(); // for logs
            addedRoleIds.ForEach((addedRoleId) =>
            {
              // Add the role if not already exists
              if (!group.GroupEligibleRoles.Any(gr => !gr.IsDeleted && gr.OrganisationEligibleRoleId == addedRoleId))
              {
                var groupAccess = new OrganisationGroupEligibleRole
                {
                  OrganisationUserGroupId = groupId,
                  OrganisationEligibleRoleId = addedRoleId
                };

                group.GroupEligibleRoles.Add(groupAccess);
              }
            });
          }
        }
      }


      // handle user update
      if (organisationGroupRequestInfo.UserInfo != null)
      {
        if (organisationGroupRequestInfo.UserInfo.AddedUserIds == null && organisationGroupRequestInfo.UserInfo.RemovedUserIds == null)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserInfo);
        }

        // Creating concatenated list of user names to take all the users at one call
        var addedUserNameList = organisationGroupRequestInfo.UserInfo.AddedUserIds == null ? new List<string>() : organisationGroupRequestInfo.UserInfo.AddedUserIds;
        var removedUserNameList = organisationGroupRequestInfo.UserInfo.RemovedUserIds == null ? new List<string>() : organisationGroupRequestInfo.UserInfo.RemovedUserIds;
        var totalUserNameList = addedUserNameList.Concat(removedUserNameList).Distinct().ToList();

        // Validate each user name
        totalUserNameList.ForEach((userName) =>
        {
          _userProfileHelperService.ValidateUserName(userName);
        });

        // Get all the users at one call
        var groupUpdatingUsers = await _dataContext.User
          .Where(u => !u.IsDeleted && u.Party.Person.OrganisationId == group.OrganisationId && totalUserNameList.Contains(u.UserName)).ToListAsync();

        // Check whether the all the usernames are actually exisitng users for organisation
        if (totalUserNameList.Count() != groupUpdatingUsers.Count())
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserInfo);
        }

        // Remove user group membership
        removedUserNameList = removedUserNameList.Distinct().ToList(); // Remove duplicates
        group.UserGroupMemberships.RemoveAll(ugm => removedUserNameList.Contains(ugm.User.UserName));
        removedUsersTupleList.AddRange(groupUpdatingUsers.Where(u => removedUserNameList.Contains(u.UserName))
          .Select(u => new Tuple<int, string>(u.Id, u.UserName)).ToList());

        // Add user group membership
        addedUserNameList = addedUserNameList.Distinct().ToList(); // Avoid duplicates
        addedUserNameList.ForEach((addedUserName) =>
        {
          // Add group for user if not already exists
          if (!group.UserGroupMemberships.Any(ugm => !ugm.IsDeleted && ugm.User != null && ugm.User.UserName == addedUserName))
          {
            var addedUserId = groupUpdatingUsers.First(u => u.UserName == addedUserName).Id;
            var userGroupMembership = new UserGroupMembership
            {
              UserId = addedUserId,
              OrganisationUserGroupId = group.Id
            };

            addedUsersTupleList.Add(new Tuple<int, string>(addedUserId, addedUserName)); // for logs
            group.UserGroupMemberships.Add(userGroupMembership);
          }
        });
        if (group.GroupType == (int)GroupType.Admin)
        {
          await CheckProfileRemoveFromGroup(organisationGroupRequestInfo, group.OrganisationId);
        }
        // this will be used in the success page. (Group success page only shows pending status for the added users)
        var expiration = new TimeSpan(0, 0, 60);
        if (addedUserNameList.Any())
        {
          _localCacheService.SetValue(groupId.ToString(), addedUserNameList, expiration);
        }
        else
        {
          if (removedUserNameList.Any())
          {
            _localCacheService.SetValue(groupId.ToString(), new List<string> { "-1"}, expiration);
          }
          else
          {
            _localCacheService.Remove(new string[] { groupId.ToString() });
          }
        }
      }
      else
      {
        var anyRoleRequiredApproval = false;

        if (addedRoleIds.Any())
        {
          anyRoleRequiredApproval = _dataContext.OrganisationEligibleRole.Include(x => x.CcsAccessRole).Any(y => y.OrganisationId == group.OrganisationId && !y.IsDeleted && addedRoleIds.Contains(y.Id)
          && y.CcsAccessRole.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired);
        }

        var expiration = new TimeSpan(0, 0, 60);
        // role removed and added where added roles may need approval 
        // or no roles are removed but new roles are added which may need approval.
        // empty list added which will not send any user's pending status to the group success page 
        if ((organisationGroupRequestInfo.RoleInfo.RemovedRoleIds != null && !anyRoleRequiredApproval) || !anyRoleRequiredApproval)
        {
          _localCacheService.SetValue(groupId.ToString(), new List<string>() { "-1"}, expiration);
        }
        else
        {
          _localCacheService.Remove(new string[] { groupId.ToString() });
        }
      }

      var mfaEnableRoleExists = orgRoleInfo.Any(r => group.GroupEligibleRoles.Any(ge => ge.OrganisationEligibleRoleId == r.Id && !ge.IsDeleted && r.MfaEnable));

      // This field should not let be updated manually as it consumes in user screen to decide mfa enable/disable
      group.MfaEnabled = mfaEnableRoleExists;
      
      await CheckMFAForGroup(group.GroupType, group.MfaEnabled);

      await _dataContext.SaveChangesAsync();

      if (mfaEnableRoleExists && (addedRoleIds.Any() || addedUsersTupleList.Any()))
      {
        await EnableMfaForUser(group);
      }

      if (_appConfigInfo.UserRoleApproval.Enable)
      {
        await RemoveGroupRolesApproveRequest(groupId, removedRoleIds);
        await RemoveGroupUsersApproveRequest(groupId, removedUsersTupleList);
        await VerifyAndCreateGroupRolePendingRequest(group, ciiOrganisationId, addedUsersTupleList, addedRoleIds);
      }
      //Modifying user roles while added in AdminGroup
      if (organisationGroupRequestInfo.UserInfo != null)
      {
        var addedUserList = organisationGroupRequestInfo.UserInfo.AddedUserIds == null ? new List<string>() : organisationGroupRequestInfo.UserInfo?.AddedUserIds;
        var removedUserList = organisationGroupRequestInfo.UserInfo.RemovedUserIds == null ? new List<string>() : organisationGroupRequestInfo.UserInfo?.RemovedUserIds;
        var modifieduserslist = addedUserList.Concat(removedUserList);
        if (modifieduserslist?.Count() > 0 && group.GroupType == (int)GroupType.Admin)
        {
          await ModifyUserRoles(organisationGroupRequestInfo, group.OrganisationId);
          await _dataContext.SaveChangesAsync();
        }
      }
      //Log
      if (hasNameChanged)
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeNameChange, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", NewGroupName:{newName}, PreviousGroupName:{previousName}");
      }
      if (addedRoleIds.Any())
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeRoleAdd, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", AddedRoleIds:{string.Join(",", addedRoleIds)}");
      }
      if (removedRoleIds.Any())
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeRoleRemove, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", RemovedRoleIds:{string.Join(",", removedRoleIds)}");
      }
      if (addedUsersTupleList.Any())
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeUserAdd, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", AddedUserIds:{string.Join(",", addedUsersTupleList.Select(au => au.Item1))}");
      }
      if (removedUsersTupleList.Any())
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeUserRemove, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", RemovedUserIds:{string.Join(",", removedUsersTupleList.Select(ru => ru.Item1))}");
      }

      var changedUsersNameList = addedUsersTupleList.Concat(removedUsersTupleList).Select(tuple => tuple.Item2).Distinct().ToList();

      // Send permission upadate email
      List<Task> emailTaskList = new();
      changedUsersNameList.ForEach(uName =>
      {
        emailTaskList.Add(_ccsSsoEmailService.SendUserPermissionUpdateEmailAsync(uName));
      });
      await Task.WhenAll(emailTaskList);

      // Invalidate redis
      var invalidatingCacheKeys = new List<string>();
      invalidatingCacheKeys.AddRange(changedUsersNameList.Select(changedUserName => $"{CacheKeyConstant.User}-{changedUserName}"));
      invalidatingCacheKeys.AddRange(existingUserNames.Select(existUserName => $"{CacheKeyConstant.User}-{existUserName}"));
      await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeys.ToArray());
    }

    private async Task EnableMfaForUser(OrganisationUserGroup group)
    {
      var mfaDisabledUsers = await _dataContext.User.Where(u => !u.IsDeleted && group.UserGroupMemberships.Select(ug => ug.UserId).Any(ugId => ugId == u.Id) && !u.MfaEnabled).ToListAsync();
      foreach (var user in mfaDisabledUsers)
      {
        user.MfaEnabled = true;

        SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
        {
          Email = user.UserName,
          MfaEnabled = true,
          SendUserRegistrationEmail = false
        };
        await _idamService.UpdateUserMfaInIdamAsync(securityApiUserInfo);
      }
      await _dataContext.SaveChangesAsync();
    }

    private async Task CheckMFAForGroup(int groupType, bool mfaEnableRoleExists)
    {
      if (groupType == (int)GroupType.Admin && !mfaEnableRoleExists)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupMFA);
      }
      else if (groupType == (int)GroupType.Other && mfaEnableRoleExists)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidGroupMFA);
      }
    }

    private async Task RemoveGroupUsersApproveRequest(int groupId, List<Tuple<int, string>> removedUsersTupleList)
    {
      if (removedUsersTupleList != null && removedUsersTupleList.Any())
      {
        var removedUserIds = removedUsersTupleList.Select(x => x.Item1);

        var userAccessRolePendingList = await _dataContext.UserAccessRolePending
          .Where(x => removedUserIds.Contains(x.UserId)
          && x.OrganisationUserGroupId == groupId).ToListAsync();

        userAccessRolePendingList.ForEach(l => { l.IsDeleted = true; l.Status = (int)UserPendingRoleStaus.Removed; });

        await _dataContext.SaveChangesAsync();
      }
    }

    private async Task RemoveGroupRolesApproveRequest(int groupId, List<int> removedRoleIds)
    {
      if (removedRoleIds != null && removedRoleIds.Any())
      {
        var userAccessRolePendingList = await _dataContext.UserAccessRolePending
          .Where(x => removedRoleIds.Contains(x.OrganisationEligibleRoleId)
          && x.OrganisationUserGroupId == groupId).ToListAsync();

        userAccessRolePendingList.ForEach(l => { l.IsDeleted = true; l.Status = (int)UserPendingRoleStaus.Removed; });

        await _dataContext.SaveChangesAsync();
      }
    }

    private async Task VerifyAndCreateGroupRolePendingRequest(OrganisationUserGroup group, string ciiOrganisationId, List<Tuple<int, string>> addedUsersTupleList, List<int> addedRoleIds)
    {
      if (!_appConfigInfo.UserRoleApproval.Enable)
      {
        return;
      }
      var org = await _dataContext.Organisation.FirstOrDefaultAsync(x => x.CiiOrganisationId == ciiOrganisationId);
      var orgDomain = org?.DomainName?.ToLower();

      List<User> newAddedUsers = new();
      if (group.GroupEligibleRoles.Any(x => !x.IsDeleted && addedRoleIds.Contains(x.OrganisationEligibleRoleId) &&
        x.OrganisationEligibleRole.CcsAccessRole.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired))
      {
        newAddedUsers = group.UserGroupMemberships.Where(x => !x.IsDeleted).Select(ugm => ugm.User).ToList();
      }
      else
      {
        newAddedUsers = group.UserGroupMemberships.Where(x => !x.IsDeleted).Select(ugm => ugm.User).Where(u => addedUsersTupleList.Select(x => x.Item1).Contains(u.Id)).ToList();
      }
      var userHasInValidDomain = newAddedUsers.Where(user => user.UserName.ToLower().Split('@')?[1] != orgDomain).ToList();

      if (userHasInValidDomain.Any())
      {
        //await RemoveGroupRolePendingRequest(group, userHasInValidDomain);

        List<int> approvalRequiredRoles = new();
        foreach (var role in group.GroupEligibleRoles.Where(x => !x.IsDeleted))
        {
          if (role.OrganisationEligibleRole.CcsAccessRole.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired)
          {
            approvalRequiredRoles.Add(role.OrganisationEligibleRoleId);
          }
        }

        if (approvalRequiredRoles.Any())
        {
          await GeneratePendingRequests(group, userHasInValidDomain, approvalRequiredRoles);
        }
        else
        {
          // remove any pending request exists for this group
          //await RemoveGroupRolePendingRequest(group);
        }
      }
      else
      {
        // remove any pending request exists for this group 
        //await RemoveGroupRolePendingRequest(group);
      }
    }

    private async Task RemoveGroupRolePendingRequest(OrganisationUserGroup group)
    {
      var pendingGroupRequest = await _dataContext.UserAccessRolePending.Where(x =>
      (x.Status == (int)UserPendingRoleStaus.Pending
      || x.Status == (int)UserPendingRoleStaus.Approved
      || x.Status == (int)UserPendingRoleStaus.Rejected) &&
      x.OrganisationUserGroupId == group.Id).ToListAsync();

      foreach (var pendingRequest in pendingGroupRequest)
      {
        pendingRequest.IsDeleted = true;
        pendingRequest.Status = (int)UserPendingRoleStaus.Removed;
      }
      await _dataContext.SaveChangesAsync();
    }

    //private async Task RemoveGroupRolePendingRequest(OrganisationUserGroup group, List<User> users)
    //{
    //    var pendingGroupRequest = await _dataContext.UserAccessRolePending.Where(x =>
    //          x.OrganisationUserGroupId == group.Id
    //          && !users.Select(user => user.Id).Contains(x.UserId)
    //          && (x.Status == (int)UserPendingRoleStaus.Pending
    //          || x.Status == (int)UserPendingRoleStaus.Approved
    //          || x.Status == (int)UserPendingRoleStaus.Rejected)).ToListAsync();

    //    foreach (var pendingRequest in pendingGroupRequest)
    //    {
    //        pendingRequest.IsDeleted = true;
    //        pendingRequest.Status = (int)UserPendingRoleStaus.Removed;
    //    }
    //    await _dataContext.SaveChangesAsync();
    //}

    public async Task<GroupUserListResponse> GetGroupUsersPendingRequestSummary(int groupId, string ciiOrgId, ResultSetCriteria resultSetCriteria, bool isPendingApproval)
    {
      var group = await _dataContext.OrganisationUserGroup
          .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User)
          .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && g.Organisation.CiiOrganisationId == ciiOrgId);

      if (group == null)
      {
        throw new ResourceNotFoundException();
      }

      var existingUserIds = group.UserGroupMemberships.Where(x => !x.IsDeleted).Select(ugm => ugm.UserId);

      var pendingAndRejectedRequests = await _dataContext.UserAccessRolePending
          .Where(x => existingUserIds.Contains(x.UserId) && x.OrganisationUserGroupId == groupId
          && (x.Status == (int)UserPendingRoleStaus.Pending || x.Status == (int)UserPendingRoleStaus.Rejected))
          .ToListAsync();

      var pendingRequests = pendingAndRejectedRequests.Where(x => x.Status == (int)UserPendingRoleStaus.Pending && !x.IsDeleted);


      var filteredUserIds = isPendingApproval ? existingUserIds.Where(x => pendingRequests.Any(y => y.UserId == x)) : existingUserIds.Where(x => !pendingAndRejectedRequests.Any(y => y.UserId == x));

      var addedUsers = _localCacheService.GetValue<List<string>>(groupId.ToString());
      if (addedUsers != null && addedUsers.Any())
      {
        var userIds = _dataContext.User.Where(x => addedUsers.Contains(x.UserName)).Select(x => x.Id);
        filteredUserIds = filteredUserIds.Where(x => userIds.Contains(x)).ToArray();
      }

      var usersQuery = _dataContext.User.Include(u => u.Party).ThenInclude(p => p.Person).Where(user => !user.IsDeleted && filteredUserIds.Contains(user.Id)).OrderBy(u => u.UserName);

      var pagedResult = await _dataContext.GetPagedResultAsync(usersQuery, resultSetCriteria);

      var groupUserListResponse = new GroupUserListResponse
      {
        groupId = groupId,
        GroupType = group.GroupType,
        CurrentPage = pagedResult.CurrentPage,
        PageCount = pagedResult.PageCount,
        RowCount = pagedResult.RowCount,
        GroupUser = pagedResult.Results?.Select(up => new GroupUser
        {
          UserId = up.UserName,
          UserPendingRoleStatus = isPendingApproval ? UserPendingRoleStaus.Pending : null, // pending and rejected will be shown as users doesn't have the role. 
          Name = $"{up.Party.Person.FirstName} {up.Party.Person.LastName}",
        }).ToList() ?? new List<GroupUser>()
      };

      return groupUserListResponse;
    }



    public async Task<OrganisationServiceRoleGroupResponseInfo> GetServiceRoleGroupAsync(string ciiOrganisationId, int groupId)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var organisationServiceRoleGroupResponseInfo = new OrganisationServiceRoleGroupResponseInfo();

      var organisationGroupResponseInfo = await this.GetGroupAsync(ciiOrganisationId, groupId);

      if (organisationGroupResponseInfo != null)
      {
        organisationServiceRoleGroupResponseInfo = ConvertGroupRoleToServiceRoleGroupResponse(organisationGroupResponseInfo);
        List<GroupServiceRoleGroup> groupServiceRoleGroups = new List<GroupServiceRoleGroup>();

        var roleIds = organisationGroupResponseInfo.Roles.Select(x => x.Id).ToList();

        var serviceRoleGroups = await _serviceRoleGroupMapperService.OrgRolesToServiceRoleGroupsAsync(roleIds);

        foreach (var serviceRoleGroup in serviceRoleGroups)
        {
          groupServiceRoleGroups.Add(new GroupServiceRoleGroup()
          {
            Id = serviceRoleGroup.Id,
            Name = serviceRoleGroup.Name,
            Description = serviceRoleGroup.Description
          });
        }

        organisationServiceRoleGroupResponseInfo.ServiceRoleGroups = groupServiceRoleGroups;
      }

      return organisationServiceRoleGroupResponseInfo;
    }

    public async Task UpdateServiceRoleGroupAsync(string ciiOrganisationId, int groupId, OrganisationServiceRoleGroupRequestInfo organisationServiceRoleGroupRequestInfo)
    {
      if (!_appConfigInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var addedServiceRoleGroupIds = organisationServiceRoleGroupRequestInfo?.ServiceRoleGroupInfo?.AddedServiceRoleGroupIds;
      var addedRoleIds = await ConvertServiceRoleGroupsToOrganisationRoleIds(ciiOrganisationId, addedServiceRoleGroupIds);

      var removedServiceRoleGroupIds = organisationServiceRoleGroupRequestInfo?.ServiceRoleGroupInfo?.RemovedServiceRoleGroupIds;
      var removedRoleIds = await ConvertServiceRoleGroupsToOrganisationRoleIds(ciiOrganisationId, removedServiceRoleGroupIds);

      OrganisationGroupRequestInfo organisationGroupRequestInfo = new OrganisationGroupRequestInfo()
      {
        GroupName = organisationServiceRoleGroupRequestInfo.GroupName,
        GroupType = organisationServiceRoleGroupRequestInfo.GroupType,
        UserInfo = organisationServiceRoleGroupRequestInfo.UserInfo,
        RoleInfo = new OrganisationGroupRolePatchInfo()
        {
          AddedRoleIds = addedRoleIds,
          RemovedRoleIds = removedRoleIds
        }
      };

      await this.UpdateGroupAsync(ciiOrganisationId, groupId, organisationGroupRequestInfo);
    }

    public async Task<OrganisationGroupServiceRoleGroupList> GetGroupsServiceRoleGroupAsync(string ciiOrganisationId, string searchString = null)
    {
      var allGroups = await GetGroupsAsync(ciiOrganisationId, searchString);
      List<OrganisationGroupServiceRoleGroupInfo> groupList = new();

      foreach (var group in allGroups.GroupList)
      {
        List<GroupServiceRoleGroup> groupServiceRoleGroups = new List<GroupServiceRoleGroup>();
        var serviceRoleGroups = await _serviceRoleGroupMapperService.OrgRolesToServiceRoleGroupsAsync(group.Roles.Select(x => x.Id).ToList());
        serviceRoleGroups = serviceRoleGroups.OrderBy(x => x.DisplayOrder).ToList();

        foreach (var serviceRoleGroup in serviceRoleGroups)
        {
          groupServiceRoleGroups.Add(new GroupServiceRoleGroup()
          {
            Id = serviceRoleGroup.Id,
            Name = serviceRoleGroup.Name,
            Description = serviceRoleGroup.Description,
            DisplayOrder = serviceRoleGroup.DisplayOrder
          });
        }

        groupList.Add(new OrganisationGroupServiceRoleGroupInfo()
        {
          GroupId = group.GroupId,
          GroupName = group.GroupName,
          GroupType = group.GroupType,
          CreatedDate = group.CreatedDate,
          ServiceRoleGroups = groupServiceRoleGroups
        });
      }

      return new OrganisationGroupServiceRoleGroupList()
      {
        OrganisationId = allGroups.OrganisationId,
        GroupList = groupList
      };
    }

    private static OrganisationServiceRoleGroupResponseInfo ConvertGroupRoleToServiceRoleGroupResponse(OrganisationGroupResponseInfo organisationGroupResponseInfo)
    {
      return new OrganisationServiceRoleGroupResponseInfo
      {
        GroupId = organisationGroupResponseInfo.GroupId,
        GroupType = organisationGroupResponseInfo.GroupType,
        MfaEnabled = organisationGroupResponseInfo.MfaEnabled,
        OrganisationId = organisationGroupResponseInfo.OrganisationId,
        GroupName = organisationGroupResponseInfo.GroupName,
        CreatedDate = organisationGroupResponseInfo.CreatedDate,
        Users = organisationGroupResponseInfo.Users
      };
    }

    private async Task<List<int>> ConvertServiceRoleGroupsToOrganisationRoleIds(string ciiOrganisationId, List<int> serviceRoleGroupIds)
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

        var organisationServiceRoleGroups = await _organisationService.GetOrganisationServiceRoleGroupsAsync(ciiOrganisationId);
        var organisationServiceRoleGroupIds = organisationServiceRoleGroups.Select(x => x.Id);

        if (!serviceRoleGroupIds.All(x => organisationServiceRoleGroupIds.Contains(x)))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidService);
        }

        List<OrganisationEligibleRole> organisationEligibleRoles = await _serviceRoleGroupMapperService.ServiceRoleGroupsToOrgRolesAsync(serviceRoleGroupIds, ciiOrganisationId);

        roleIds = organisationEligibleRoles.Select(x => x.Id).ToList();
      }
      else
      {
        roleIds = new List<int>();
      }

      return roleIds;
    }

    private async Task GeneratePendingRequests(OrganisationUserGroup group, List<User> userHasInValidDomain, List<int> approvalRequiredRoles)
    {
      var existingUsersRequests = await _dataContext.UserAccessRolePending.Where(x => approvalRequiredRoles.Contains(x.OrganisationEligibleRoleId)
                    && x.OrganisationUserGroupId == group.Id
                    && userHasInValidDomain.Select(u => u.Id).Contains(x.UserId)).ToListAsync();

      foreach (var user in userHasInValidDomain)
      {
        var latestRequestOfUser = existingUsersRequests.OrderByDescending(x => x.Id).FirstOrDefault(x => x.UserId == user.Id);

        if (latestRequestOfUser == null ||
          (latestRequestOfUser.Status != (int)UserPendingRoleStaus.Pending
          && latestRequestOfUser.Status != (int)UserPendingRoleStaus.Approved
          && latestRequestOfUser.Status != (int)UserPendingRoleStaus.Rejected))
        {
          await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(new UserProfileEditRequestInfo
          {
            UserName = user.UserName,
            OrganisationId = group.Organisation.CiiOrganisationId,
            Detail = new UserRequestDetail
            {
              GroupId = group.Id,
              GroupType = group.GroupType,
              RoleIds = approvalRequiredRoles
            }
          }, sendEmailNotification: false);
        }
      }
    }
    private async Task CheckProfileRemoveFromGroup(OrganisationGroupRequestInfo organisationGroupRequestInfo, int organisationId)
    {
      if (organisationGroupRequestInfo.UserInfo != null)
      {
        if (organisationGroupRequestInfo.UserInfo.RemovedUserIds.Count > 0)
        {
          var groupUpdatingUsers = await GetUsers(organisationGroupRequestInfo.UserInfo.RemovedUserIds, organisationId);

          if (groupUpdatingUsers == null)
          {
            throw new ResourceNotFoundException();
          }
          //Cannot remove myprofile from the admin group 
          if (groupUpdatingUsers.Any(x => x.Id == _requestContext.UserId))
          {
            throw new CcsSsoException(ErrorConstant.ErrorCannotRemoveMyProfile);
          }

        }
      }
    }
    private async Task ModifyUserRoles(OrganisationGroupRequestInfo organisationGroupRequestInfo, int organisationId)
    {
      var orgAdminAccessRoleId =await _externalHelperService.GetOrganisationAdminAccessRoleId(organisationId);
      if (organisationGroupRequestInfo.UserInfo?.AddedUserIds.Count > 0)
      {
        await AddAdminRole(organisationGroupRequestInfo, orgAdminAccessRoleId, organisationId);
      }
      if (organisationGroupRequestInfo.UserInfo?.RemovedUserIds.Count > 0)
      {
        await RemoveAdminRole(organisationGroupRequestInfo, organisationId);
      }
    }

    private async Task RemoveAdminRole(OrganisationGroupRequestInfo organisationGroupRequestInfo, int organisationId)
    {
      var groupUpdatingUsers = await GetUsers(organisationGroupRequestInfo.UserInfo.RemovedUserIds, organisationId);

      if (groupUpdatingUsers == null)
      {
        throw new ResourceNotFoundException();
      }
      //Cannot remove myprofile from the admin group 
      if (groupUpdatingUsers.Any(x => x.Id == _requestContext.UserId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorCannotRemoveMyProfile);
      }
      // Remove the admin access role from the user
      foreach (var user in groupUpdatingUsers)
      {
        if (user.UserAccessRoles != null)
        {
          var adminAccessRoleInfo = user.UserAccessRoles.FirstOrDefault(uar => !uar.IsDeleted
           && uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey);
          if (adminAccessRoleInfo != null)
          {
            adminAccessRoleInfo.IsDeleted = true;
          }
        }
      }
    }

    private async Task AddAdminRole(OrganisationGroupRequestInfo organisationGroupRequestInfo, int orgRoleId, int organisationId)
    {
      var groupUpdatingUsers = await GetUsers(organisationGroupRequestInfo.UserInfo.AddedUserIds, organisationId);
      if (groupUpdatingUsers == null)
      {
        throw new ResourceNotFoundException();
      }
      if (groupUpdatingUsers.Any(x => x.Id == _requestContext.UserId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorCannotAddMyProfile);
      }
      foreach (var user in groupUpdatingUsers)
      {
        if (user.UserAccessRoles == null)
        {
          user.UserAccessRoles = new List<UserAccessRole>();
        }
        if (user.UserAccessRoles.Any(x => x.OrganisationEligibleRoleId == orgRoleId && !x.IsDeleted))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
        }
        user.UserAccessRoles.Add(new UserAccessRole
        {
          UserId = user.Id,
          OrganisationEligibleRoleId = orgRoleId
        }); ;
      }
    }

    private async Task CheckInvalidRoleInfo(List<int> roleIds)
    {
      var ccsRolekey = await _dataContext.OrganisationEligibleRole.Where(orgRole => !orgRole.IsDeleted && roleIds.Any(roleid => roleid == orgRole.Id)).Select(x => x.CcsAccessRole.CcsAccessRoleNameKey).Distinct().ToListAsync();
      if (ccsRolekey.Contains(Contstant.OrgAdminRoleNameKey) || ccsRolekey.Contains(Contstant.FleetPortalUserRoleNameKey))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }
    }
    private async Task CheckInvalidAdminGroupNameInfo(OrganisationGroupRequestInfo organisationGroupRequestInfo,int groupType)
    {
      if (groupType == (int)GroupType.Admin && (organisationGroupRequestInfo.GroupName != null
        || !String.IsNullOrWhiteSpace(organisationGroupRequestInfo.GroupName)))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserGroup);
      }
    }
    private async Task CheckInvalidAdminGroupRoleInfo(OrganisationGroupRequestInfo organisationGroupRequestInfo,int groupType)
    {
      var roleIds = organisationGroupRequestInfo.RoleInfo?.AddedRoleIds.Concat(organisationGroupRequestInfo.RoleInfo.RemovedRoleIds).ToList();
      if (groupType == (int)GroupType.Admin && roleIds?.Count > 0)
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
      }
    }
    private async Task<List<User>> GetUsers(List<string> UserIds , int organisationId)
    {
      var users = await _dataContext.User
       .Include(u => u.UserAccessRoles).ThenInclude(uar => uar.OrganisationEligibleRole).ThenInclude(oer => oer.CcsAccessRole)
       .Where(u => !u.IsDeleted && u.Party.Person.OrganisationId == organisationId
        && UserIds.Contains(u.UserName))
       .ToListAsync();
      return users;
    }

  }
}


