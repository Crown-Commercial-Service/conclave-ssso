using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
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

    public OrganisationGroupService(IDataContext dataContext, IUserProfileHelperService userProfileHelperService,
      IAuditLoginService auditLoginService)
    {
      _dataContext = dataContext;
      _userProfileHelperService = userProfileHelperService;
      _auditLoginService = auditLoginService;
    }

    public async Task<int> CreateGroupAsync(string ciiOrganisationId, OrganisationGroupNameInfo organisationGroupNameInfo)
    {

      if (string.IsNullOrWhiteSpace(organisationGroupNameInfo.GroupName))
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
        .Include(g => g.UserGroupMemberships)
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
    }

    public async Task<OrganisationGroupResponseInfo> GetGroupAsync(string ciiOrganisationId, int groupId)
    {
      var group = await _dataContext.OrganisationUserGroup
        .Include(g => g.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User).ThenInclude(u => u.Party).ThenInclude(p => p.Person)
        .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && !g.Organisation.IsDeleted && g.Organisation.CiiOrganisationId == ciiOrganisationId);

      if (group == null)
      {
        throw new ResourceNotFoundException();
      }

      OrganisationGroupResponseInfo organisationGroupResponseInfo = new OrganisationGroupResponseInfo
      {
        GroupId = group.Id,
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
          Name = $"{ugm.User.Party.Person.FirstName} {ugm.User.Party.Person.LastName}"
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
        .Include(g => g.GroupEligibleRoles)
        .Include(g => g.UserGroupMemberships).ThenInclude(ugm => ugm.User)
        .FirstOrDefaultAsync(g => !g.IsDeleted && g.Id == groupId && g.Organisation.CiiOrganisationId == ciiOrganisationId);

      if (group == null)
      {
        throw new ResourceNotFoundException();
      }

      string newName = string.Empty;
      string previousName = group.UserGroupName;
      bool hasNameChanged = false;
      List<int> addedRoleIds = new();
      List<int> removedRoleIds = new();
      List<int> addedUserIds = new();
      List<int> removedUserIds = new();

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

      if (organisationGroupRequestInfo.RoleInfo != null)
      {
        if (organisationGroupRequestInfo.RoleInfo.AddedRoleIds == null && organisationGroupRequestInfo.RoleInfo.RemovedRoleIds == null)
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidRoleInfo);
        }

        // Take the not deleted org role ids
        var orgRoleIds = await _dataContext.OrganisationEligibleRole
          .Where(r => !r.IsDeleted)
          .Select(r => r.Id)
          .ToListAsync();

        if ((organisationGroupRequestInfo.RoleInfo.AddedRoleIds != null &&
          organisationGroupRequestInfo.RoleInfo.AddedRoleIds.Any(id => !orgRoleIds.Contains(id)))
          || (organisationGroupRequestInfo.RoleInfo.RemovedRoleIds != null &&
          organisationGroupRequestInfo.RoleInfo.RemovedRoleIds.Any(id => !orgRoleIds.Contains(id))))
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
        group.UserGroupMemberships.RemoveAll(ugm => removedUserNameList.Contains(ugm.User.UserName));
        removedUserIds.AddRange(groupUpdatingUsers.Where(u => removedUserNameList.Contains(u.UserName)).Select(u => u.Id).Distinct().ToList());

        // Add user group membership
        addedUserNameList = addedUserNameList.Distinct().ToList(); // Avoid duplicates
        addedUserNameList.ForEach((addedUserName) =>
        {
          // Add group for user if not already exists
          if (!group.UserGroupMemberships.Any(ugm => !ugm.IsDeleted && ugm.User!= null && ugm.User.UserName == addedUserName))
          {
            var addedUserId = groupUpdatingUsers.First(u => u.UserName == addedUserName).Id;
            var userGroupMembership = new UserGroupMembership
            {
              UserId = addedUserId,
              OrganisationUserGroupId = group.Id
            };

            addedUserIds.Add(addedUserId); // for logs
            group.UserGroupMemberships.Add(userGroupMembership);
          }
        });
      }

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
      if (addedUserIds.Any())
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeUserAdd, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", AddedUserIds:{string.Join(",", addedUserIds)}");
      }
      if (removedUserIds.Any())
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.GroupeUserRemove, AuditLogApplication.ManageGroup, $"GroupId:{group.Id}, OrganisationId:{ciiOrganisationId}" +
          $", RemovedUserIds:{string.Join(",", removedUserIds)}");
      }
    }
  }
}
