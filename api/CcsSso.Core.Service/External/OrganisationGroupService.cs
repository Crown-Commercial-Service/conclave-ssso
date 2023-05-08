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
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public OrganisationGroupService(IDataContext dataContext, IUserProfileHelperService userProfileHelperService,
      IAuditLoginService auditLoginService, ICcsSsoEmailService ccsSsoEmailService, IWrapperCacheService wrapperCacheService,
      ApplicationConfigurationInfo appConfigInfo, IServiceRoleGroupMapperService serviceRoleGroupMapperService,
      IOrganisationProfileService organisationService)
    {
      _dataContext = dataContext;
      _userProfileHelperService = userProfileHelperService;
      _auditLoginService = auditLoginService;
      _ccsSsoEmailService = ccsSsoEmailService;
      _wrapperCacheService = wrapperCacheService;
      _appConfigInfo = appConfigInfo;
      _serviceRoleGroupMapperService = serviceRoleGroupMapperService;
      _organisationService = organisationService;
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

      group.IsDeleted = true;
      group.GroupEligibleRoles.ForEach((groupRoles) => { groupRoles.IsDeleted = true; });
      group.UserGroupMemberships.ForEach((groupMembership) => { groupMembership.IsDeleted = true; });

      await _dataContext.SaveChangesAsync();

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

      OrganisationGroupResponseInfo organisationGroupResponseInfo = new OrganisationGroupResponseInfo
      {
        GroupId = group.Id,
        MfaEnabled = group.MfaEnabled,
        OrganisationId = ciiOrganisationId,
        GroupName = group.UserGroupName,
        CreatedDate = group.CreatedOnUtc.Date.ToString(DateTimeFormat.DateFormatShortMonth),
        Roles = group.GroupEligibleRoles.Where(gr => !gr.IsDeleted).Select(gr => new GroupRole
        {
          Id = gr.OrganisationEligibleRole.Id,
          Name = gr.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName
        }).ToList(),
        Users = group.UserGroupMemberships.Where(ugm => !ugm.IsDeleted).Select(ugm => new GroupUser
        {
          UserId = ugm.User.UserName,
          Name = $"{ugm.User.Party.Person.FirstName} {ugm.User.Party.Person.LastName}",
          IsAdmin = ugm.User.UserAccessRoles.Any(r => !r.IsDeleted && r.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey && !r.OrganisationEligibleRole.IsDeleted)
        }).ToList()
      };

      return organisationGroupResponseInfo;
    }

    public async Task<OrganisationGroupList> GetGroupsAsync(string ciiOrganisationId, string searchString = null)
    {
      var organisation = await _dataContext.Organisation
       .Include(o => o.UserGroups)
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
          CreatedDate = g.CreatedOnUtc.Date.ToString(DateTimeFormat.DateFormatShortMonth)
        }).OrderBy(g => g.GroupName).ToList();

      return new OrganisationGroupList
      {
        OrganisationId = organisation.CiiOrganisationId,
        GroupList = groups
      };
    }

    public async Task UpdateGroupAsync(string ciiOrganisationId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo)
    {
      var group = await _dataContext.OrganisationUserGroup
        .Include(g => g.GroupEligibleRoles).ThenInclude(r => r.OrganisationEligibleRole)
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
      }

      var mfaEnableRoleExists = orgRoleInfo.Any(r => group.GroupEligibleRoles.Any(ge => ge.OrganisationEligibleRoleId == r.Id && !ge.IsDeleted && r.MfaEnable));

      // validate for mfa
      if (mfaEnableRoleExists && (addedRoleIds.Any() || addedUsersTupleList.Any()))
      {
        var mfaDisabledUserExists = await _dataContext.User.AnyAsync(u => !u.IsDeleted && group.UserGroupMemberships.Select(ug => ug.UserId).Any(ugId => ugId == u.Id) && !u.MfaEnabled);

        if (mfaDisabledUserExists)
        {
          throw new CcsSsoException("MFA_DISABLED_USERS_INCLUDED");
        }
      }
      // This field should not let be updated manually as it consumes in user screen to decide mfa enable/disable
      group.MfaEnabled = mfaEnableRoleExists;
      await _dataContext.SaveChangesAsync();

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
        UserInfo = organisationServiceRoleGroupRequestInfo.UserInfo,
        RoleInfo = new OrganisationGroupRolePatchInfo()
        {
          AddedRoleIds = addedRoleIds,
          RemovedRoleIds = removedRoleIds
        }
      };

      await this.UpdateGroupAsync(ciiOrganisationId, groupId, organisationGroupRequestInfo);
    }

    private static OrganisationServiceRoleGroupResponseInfo ConvertGroupRoleToServiceRoleGroupResponse(OrganisationGroupResponseInfo organisationGroupResponseInfo)
    {
      return new OrganisationServiceRoleGroupResponseInfo
      {
        GroupId = organisationGroupResponseInfo.GroupId,
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
  }
}
