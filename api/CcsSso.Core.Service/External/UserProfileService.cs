using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Services.Helpers;
using CcsSso.Shared.Domain.Contexts;
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
    public UserProfileService(IDataContext dataContext, IUserProfileHelperService userHelper,
      RequestContext requestContext, IIdamService idamService, ICcsSsoEmailService ccsSsoEmailService)
    {
      _dataContext = dataContext;
      _userHelper = userHelper;
      _requestContext = requestContext;
      _idamService = idamService;
      _ccsSsoEmailService = ccsSsoEmailService;
    }

    public async Task<UserEditResponseInfo> CreateUserAsync(UserProfileEditRequestInfo userProfileRequestInfo)
    {

      var isRegisteredInIdam = false;
      _userHelper.ValidateUserName(userProfileRequestInfo.UserName);

      var organisation = await _dataContext.Organisation
        .Include(o => o.UserGroups)
        .Include(o => o.OrganisationEligibleRoles)
        .Include(o => o.OrganisationEligibleIdentityProviders)
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

      Validate(userProfileRequestInfo, false, organisation);

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
          UserAccessRoles = userAccessRoles,
          OrganisationEligibleIdentityProviderId = userProfileRequestInfo.Detail.IdentityProviderId
        }
      };

      _dataContext.Party.Add(party);

      await _dataContext.SaveChangesAsync();

      var eligibleIdentityProvider = await _dataContext.OrganisationEligibleIdentityProvider
        .Include(x => x.IdentityProvider)
        .FirstOrDefaultAsync(i => i.Id == userProfileRequestInfo.Detail.IdentityProviderId);
      if (eligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName)
      {
        SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
        {
          Email = userProfileRequestInfo.UserName,
          UserName = userProfileRequestInfo.UserName,
          FirstName = userProfileRequestInfo.FirstName,
          LastName = userProfileRequestInfo.LastName
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
      else
      {
        // Send the welcome email if not Idam user. (Idam users will recieve an email while registering)
        await _ccsSsoEmailService.SendUserWelcomeEmailAsync(party.User.UserName, eligibleIdentityProvider.IdentityProvider.IdpName);
      }

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
        .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .ThenInclude(pr => pr.Organisation)
        .Include(u => u.OrganisationEligibleIdentityProvider).ThenInclude(oi => oi.IdentityProvider)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user != null)
      {
        var userProfileInfo = new UserProfileResponseInfo
        {
          UserName = user.UserName,
          OrganisationId = user.Party.Person.Organisation.CiiOrganisationId,
          FirstName = user.Party.Person.FirstName,
          LastName = user.Party.Person.LastName,
          Title = (UserTitle)user.UserTitle,
          Detail = new UserResponseDetail
          {
            Id = user.Id,
            IdentityProvider = user.OrganisationEligibleIdentityProvider.IdentityProvider?.IdpConnectionName,
            CanChangePassword = user.OrganisationEligibleIdentityProvider.IdentityProvider?.IdpConnectionName == Contstant.ConclaveIdamConnectionName,
            IdentityProviderId = user.OrganisationEligibleIdentityProviderId,
            IdentityProviderDisplayName = user.OrganisationEligibleIdentityProvider.IdentityProvider?.IdpName,
            GroupIds = user.UserGroupMemberships != null ? user.UserGroupMemberships.Where(x => !x.IsDeleted).Select(m => m.OrganisationUserGroupId).ToList() : new List<int>(),
            UserGroups = new List<GroupAccessRole>(),
            RoleIds = user.UserAccessRoles.Where(x => !x.IsDeleted).Select(ar => ar.OrganisationEligibleRoleId).ToList(),
            RoleNames = user.UserAccessRoles.Where(x => !x.IsDeleted).Select(ar => ar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey).ToList()
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

    public async Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, string searchString = null)
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
        .Where(u => !u.IsDeleted && u.Id != _requestContext.UserId &&
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

    public async Task DeleteUserAsync(string userName)
    {

      _userHelper.ValidateUserName(userName);

      var user = await _dataContext.User
        .Include(u => u.Party).ThenInclude(p => p.Person)
        .Include(u => u.UserGroupMemberships)
        .Include(u => u.UserAccessRoles)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      if (await IsOrganisationOnlyAdminAsync(user, userName))
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

      await _dataContext.SaveChangesAsync();

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
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      bool isMyProfile = _requestContext.UserId == user.Id;

      Validate(userProfileRequestInfo, isMyProfile, organisation);

      bool hasProfileInfoChanged = (user.Party.Person.FirstName != userProfileRequestInfo.FirstName.Trim() ||
                                    user.Party.Person.LastName != userProfileRequestInfo.LastName.Trim() ||
                                    user.UserTitle != (int)userProfileRequestInfo.Title ||
                                    user.OrganisationEligibleIdentityProviderId != userProfileRequestInfo.Detail.IdentityProviderId);

      user.Party.Person.FirstName = userProfileRequestInfo.FirstName.Trim();
      user.Party.Person.LastName = userProfileRequestInfo.LastName.Trim();
      bool hasGroupMembershipsNotChanged = true;
      bool hasRolesNotChanged = true;
      if (!isMyProfile)
      {
        user.UserTitle = (int)userProfileRequestInfo.Title;
        var requestGroups = userProfileRequestInfo.Detail.GroupIds == null ? new List<int>() : userProfileRequestInfo.Detail.GroupIds.OrderBy(e => e).ToList();
        var requestRoles = userProfileRequestInfo.Detail.RoleIds == null ? new List<int>() : userProfileRequestInfo.Detail.RoleIds.OrderBy(e => e).ToList();
        hasGroupMembershipsNotChanged = Enumerable.SequenceEqual(requestGroups, user.UserGroupMemberships.Select(ug => ug.OrganisationUserGroup.Id).OrderBy(e => e));
        hasRolesNotChanged = Enumerable.SequenceEqual(requestRoles, user.UserAccessRoles.Select(ur => ur.OrganisationEligibleRoleId).OrderBy(e => e));
        user.UserGroupMemberships.RemoveAll(g => true);
        user.UserAccessRoles.RemoveAll(r => true);

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

        var previousIdentityProviderId = user.OrganisationEligibleIdentityProviderId;
        user.OrganisationEligibleIdentityProviderId = userProfileRequestInfo.Detail.IdentityProviderId;

        if (previousIdentityProviderId != userProfileRequestInfo.Detail.IdentityProviderId)
        {
          var elegibleIdentityProviders = await _dataContext.OrganisationEligibleIdentityProvider
            .Include(x => x.IdentityProvider)
            .ToListAsync();

          if (elegibleIdentityProviders.First(i => i.Id == previousIdentityProviderId).IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName)
          {
            await _idamService.DeleteUserInIdamAsync(userName);
          }
          else if (elegibleIdentityProviders.First(i => i.Id == userProfileRequestInfo.Detail.IdentityProviderId).IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName)
          {
            SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
            {
              Email = userName,
              UserName = userName,
              FirstName = userProfileRequestInfo.FirstName,
              LastName = userProfileRequestInfo.LastName
            };

            await _idamService.RegisterUserInIdamAsync(securityApiUserInfo);
            isRegisteredInIdam = true;
          }
        }
      }

      await _dataContext.SaveChangesAsync();

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

    public async Task<UserEditResponseInfo> UpdateUserRolesAsync(string userName, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      var isRegisteredInIdam = false;
      // _userHelper.ValidateUserName(userName);

      var organisation = await _dataContext.Organisation
        //.Include(o => o.UserGroups)
        //.ThenInclude(ug => ug.GroupEligibleRoles)
        .Include(gr => gr.OrganisationEligibleRoles)
        .ThenInclude(or => or.CcsAccessRole)
        // .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
        // .Include(o => o.OrganisationEligibleIdentityProviders)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      //var user = await _dataContext.User
      //  // .Include(u => u.Party).ThenInclude(p => p.Person)
      //  // .Include(u => u.UserGroupMemberships)
      //  // .Include(u => u.UserAccessRoles)
      //  .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      //if (user == null)
      //{
      //  throw new ResourceNotFoundException();
      //}

      // bool isMyProfile = _requestContext.UserId == user.Id;
      // if (!isMyProfile)
      // {
      //userProfileRequestInfo.GroupIds?.ForEach((groupId) =>
      //{
      //  var groups = _dataContext.UserGroupMembership
      //  .Include(i => i.OrganisationUserGroup)
      //  .ThenInclude(i => i.GroupEligibleRoles)
      //  .ThenInclude(i => i.OrganisationEligibleRole)
      //  .ThenInclude(i => i.CcsAccessRole)
      //  .Where(u => u.UserId == userProfileRequestInfo.Id)
      //  .SelectMany(r => r.OrganisationUserGroup.GroupEligibleRoles.Where(d => d.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR"))
      //  .ToList();

      //  groups.ForEach((g) => {
      //    g.IsDeleted = true;
      //  });
      //    //var group = _dataContext.OrganisationGroupEligibleRole
      //    //.Include(i => i.OrganisationEligibleRole)
      //    //.ThenInclude(i => i.CcsAccessRole)
      //    //.FirstOrDefault(r => r.OrganisationUserGroupId == groupId && r.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");

      //  //  if (group != null)
      //  //{
      //  //  group.IsDeleted = true;
      //  //  // _dataContext.OrganisationGroupEligibleRole.Remove(group);
      //  //}
      //});

      var groups = _dataContext.UserGroupMembership
          .Include(i => i.OrganisationUserGroup)
          .ThenInclude(i => i.GroupEligibleRoles)
          .ThenInclude(i => i.OrganisationEligibleRole)
          .ThenInclude(i => i.CcsAccessRole)
          .Where(u => u.UserId == userProfileRequestInfo.Detail.Id)
          .Where(r => r.OrganisationUserGroup.GroupEligibleRoles.Any(d => !d.IsDeleted && d.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR"))
          .ToList();

      groups.ForEach((g) =>
      {
        g.IsDeleted = true;
      });


      //var ugms = _dataContext.UserGroupMembership.Where(u => u.UserId == userProfileRequestInfo.Id).ToList();
      //var orgUserGroups = _dataContext.OrganisationUserGroup.Include(i => i.GroupEligibleRoles).ToList();
      //orgUserGroups.Where(d => d.GroupEligibleRoles..CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");
      //ugms.ForEach((u) => {
      //  // u.IsDeleted = true;


      //});

      var role = _dataContext.UserAccessRole
          .Include(i => i.User)
          .Include(i => i.OrganisationEligibleRole)
          .ThenInclude(i => i.CcsAccessRole)
          .FirstOrDefault(r => r.User.Id == userProfileRequestInfo.Detail.Id && r.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");

      if (role != null)
      {
        role.IsDeleted = true;
        // _dataContext.UserAccessRole.Remove(role);
      }
      // }

      await _dataContext.SaveChangesAsync();

      return new UserEditResponseInfo
      {
        UserId = userName,
        IsRegisteredInIdam = isRegisteredInIdam
      };
    }

    public async Task<UserEditResponseInfo> AddAdminRoleAsync(string userName, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      var isRegisteredInIdam = false;
      // _userHelper.ValidateUserName(userName);

      var organisation = await _dataContext.Organisation
        //.Include(o => o.UserGroups)
        //.ThenInclude(ug => ug.GroupEligibleRoles)
        .Include(gr => gr.OrganisationEligibleRoles)
        .ThenInclude(or => or.CcsAccessRole)
        // .Include(o => o.OrganisationEligibleRoles).ThenInclude(or => or.CcsAccessRole)
        // .Include(o => o.OrganisationEligibleIdentityProviders)
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == userProfileRequestInfo.OrganisationId);

      if (organisation == null)
      {
        throw new ResourceNotFoundException();
      }

      var user = await _dataContext.User
        //.Include(u => u.Party).ThenInclude(p => p.Person)
        //.Include(u => u.UserGroupMemberships)
        //.Include(u => u.UserAccessRoles)
        .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      if (user == null)
      {
        throw new ResourceNotFoundException();
      }

      // bool isMyProfile = _requestContext.UserId == user.Id;
      // if (!isMyProfile)
      // {
      var usr = _dataContext.User.FirstOrDefault(r => !r.IsDeleted && r.UserName == userProfileRequestInfo.UserName);
      //var role = _dataContext.OrganisationEligibleRole
      //  .Include(i => i.CcsAccessRole)
      //  .Include(i => i.Organisation)
      //  .FirstOrDefault(r => r.Organisation.CiiOrganisationId == userProfileRequestInfo.OrganisationId && r.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");
      var role = organisation.OrganisationEligibleRoles.FirstOrDefault(x => !x.IsDeleted && !x.CcsAccessRole.IsDeleted && x.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR");
      _dataContext.UserAccessRole.Add(new UserAccessRole
      {
        User = usr,
        OrganisationEligibleRole = role,
      });
      await _dataContext.SaveChangesAsync();
      // }

      // await _dataContext.SaveChangesAsync();

      return new UserEditResponseInfo
      {
        UserId = userName,
        IsRegisteredInIdam = isRegisteredInIdam
      };
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
      if (string.IsNullOrWhiteSpace(userProfileReqestInfo.FirstName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidFirstName);
      }

      if (string.IsNullOrWhiteSpace(userProfileReqestInfo.LastName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidLastName);
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

        if (userProfileReqestInfo.Title == null || !UtilitiesHelper.IsEnumValueValid<UserTitle>((int)userProfileReqestInfo.Title))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidTitle);
        }

        if ((userProfileReqestInfo.Detail.GroupIds == null && userProfileReqestInfo.Detail.RoleIds == null) // Both null
          || (userProfileReqestInfo.Detail.GroupIds == null && userProfileReqestInfo.Detail.RoleIds != null && !userProfileReqestInfo.Detail.RoleIds.Any()) // Group null role empty
          || (userProfileReqestInfo.Detail.GroupIds != null && !userProfileReqestInfo.Detail.GroupIds.Any() && userProfileReqestInfo.Detail.RoleIds == null)  // Group empty role null
           || (userProfileReqestInfo.Detail.GroupIds != null && !userProfileReqestInfo.Detail.GroupIds.Any() &&
                userProfileReqestInfo.Detail.RoleIds != null && !userProfileReqestInfo.Detail.RoleIds.Any())) // Both empty
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidUserGroupRole);
        }
        else
        {
          if (userProfileReqestInfo.Detail.GroupIds != null && userProfileReqestInfo.Detail.GroupIds.Any(gId => !orgGroupIds.Contains(gId)))
          {
            throw new CcsSsoException(ErrorConstant.ErrorInvalidUserGroup);
          }

          if (userProfileReqestInfo.Detail.RoleIds != null && userProfileReqestInfo.Detail.RoleIds.Any(gId => !orgRoleIds.Contains(gId)))
          {
            throw new CcsSsoException(ErrorConstant.ErrorInvalidUserRole);
          }
        }

        if (!orgIdpIds.Any(orgIdpId => orgIdpId == userProfileReqestInfo.Detail.IdentityProviderId))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidIdentityProvider);
        }
      }
    }


  }
}
