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
  public class UserProfileService : IUserProfileService
  {
    private readonly IDataContext _dataContext;
    private readonly IUserProfileHelperService _userHelper;
    public UserProfileService(IDataContext dataContext, IUserProfileHelperService userHelper)
    {
      _dataContext = dataContext;
      _userHelper = userHelper;
    }

    public async Task<string> CreateUserAsync(UserProfileRequestInfo userProfileRequestInfo)
    {
      var organisation = await _dataContext.Organisation
        .Include(o => o.UserGroups)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);
      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var user = await _dataContext.User
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userProfileRequestInfo.UserName);

      if (user != null)
      {
        throw new ResourceAlreadyExistsException();
      }

      var groupIds = organisation.UserGroups.Select(u => u.Id).ToList();

      await ValidateAsync(userProfileRequestInfo, false, groupIds);

      var userGroupMemberships = new List<UserGroupMembership>();
      userProfileRequestInfo.GroupIds.ForEach((groupId) =>
      {
        userGroupMemberships.Add(new UserGroupMembership
        {
          OrganisationUserGroupId = groupId
        });
      });

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
          UserName = userProfileRequestInfo.UserName,
          UserTitle = (int)userProfileRequestInfo.Title,
          UserGroupMemberships = userGroupMemberships,
          IdentityProviderId = userProfileRequestInfo.IdentityProviderId
        }
      };

      _dataContext.Party.Add(party);

      await _dataContext.SaveChangesAsync();

      return party.User.UserName;
    }

    public async Task<UserProfileResponseInfo> GetUserAsync(string userName)
    {
      _userHelper.ValidateUserName(userName);

      var user = await _dataContext.User
        .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
        .ThenInclude(oug => oug.GroupAccesses).ThenInclude(ga => ga.CcsAccessRole)
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .ThenInclude(pr => pr.Organisation)
        .Include(u => u.IdentityProvider)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user != null)
      {
        var userProfileInfo = new UserProfileResponseInfo
        {
          Id = user.Id,
          FirstName = user.Party.Person.FirstName,
          LastName = user.Party.Person.LastName,
          UserName = user.UserName,
          Title = (UserTitle)user.UserTitle,
          IdentityProvider = user.IdentityProvider?.IdpConnectionName,
          CanChangePassword = user.IdentityProvider?.IdpConnectionName == Contstant.ConclaveIdamConnectionName,
          IdentityProviderId = user.IdentityProviderId,
          IdentityProviderDisplayName = user.IdentityProvider?.IdpName,
          GroupIds = user.UserGroupMemberships != null ? user.UserGroupMemberships.Select(m => m.OrganisationUserGroupId).ToList() : new List<int>(),
          OrganisationId = user.Party.Person.Organisation.CiiOrganisationId,
          UserGroups = new List<GroupAccessRole>()
        };

        if (user.UserGroupMemberships != null)
        {
          foreach (var userGroupMembership in user.UserGroupMemberships)
          {
            if (!userGroupMembership.IsDeleted && userGroupMembership.OrganisationUserGroup.GroupAccesses != null)
            {
              foreach (var groupAccess in userGroupMembership.OrganisationUserGroup.GroupAccesses)
              {
                var groupAccessRole = new GroupAccessRole
                {
                  Group = userGroupMembership.OrganisationUserGroup.UserGroupName,
                  AccessRole = groupAccess.CcsAccessRole.CcsAccessRoleName
                };

                userProfileInfo.UserGroups.Add(groupAccessRole);
              }
            }
          }
        }

        return userProfileInfo;
      }

      throw new ResourceNotFoundException();
    }

    public async Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, string userName = null)
    {

      if (!await _dataContext.Organisation.AnyAsync(o => !o.IsDeleted && o.CiiOrganisationId == organisationId))
      {
        throw new ResourceNotFoundException();
      }

      // TODO :- Exclude the logged in user
      var userPagedInfo = await _dataContext.GetPagedResultAsync(_dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .Where(u => !u.IsDeleted && u.Party.Person.Organisation.CiiOrganisationId == organisationId &&
        (string.IsNullOrWhiteSpace(userName) || u.UserName.Contains(userName)))
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

    public async Task UpdateUserAsync(string userName, bool isMyProfile, UserProfileRequestInfo userProfileRequestInfo)
    {
      _userHelper.ValidateUserName(userName);

      var organisation = await _dataContext.Organisation
        .Include(o => o.UserGroups)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);
      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var groupIds = organisation.UserGroups.Select(u => u.Id).ToList();

      await ValidateAsync(userProfileRequestInfo, isMyProfile, groupIds);

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .Include(u => u.UserGroupMemberships)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user != null)
      {
        user.UserTitle = (int)userProfileRequestInfo.Title;
        user.Party.Person.FirstName = userProfileRequestInfo.FirstName.Trim();
        user.Party.Person.LastName = userProfileRequestInfo.LastName.Trim();

        if (!isMyProfile)
        {
          user.IdentityProviderId = userProfileRequestInfo.IdentityProviderId;

          user.UserGroupMemberships.RemoveAll(g => true);
          var userGroupMemberships = new List<UserGroupMembership>();
          userProfileRequestInfo.GroupIds.ForEach((groupId) =>
          {
            userGroupMemberships.Add(new UserGroupMembership
            {
              OrganisationUserGroupId = groupId
            });
          });
          user.UserGroupMemberships = userGroupMemberships;
        }

        await _dataContext.SaveChangesAsync();
      }
      else
      {
        throw new ResourceNotFoundException();
      }
    }

    private async Task ValidateAsync(UserProfileRequestInfo userProfileReqestInfo, bool isMyProfile, List<int> groupIds)
    {
      if (string.IsNullOrWhiteSpace(userProfileReqestInfo.FirstName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidFirstName);
      }

      if (string.IsNullOrWhiteSpace(userProfileReqestInfo.LastName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidLastName);
      }

      if (!isMyProfile && (userProfileReqestInfo.GroupIds == null || userProfileReqestInfo.GroupIds.Any(gId => !groupIds.Contains(gId))))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserGroup);
      }

      if (!isMyProfile && !await _dataContext.IdentityProvider.AnyAsync(idp => idp.Id == userProfileReqestInfo.IdentityProviderId))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidIdentityProvider);
      }
    }
  }
}
