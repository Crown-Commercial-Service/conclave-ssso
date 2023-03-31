using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Service
{
  public class UserService : IUserService
  {
    private readonly IDataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly IUserProfileHelperService _userHelper;
    private readonly IServiceRoleGroupMapperService _serviceRoleGroupMapperService;

    public UserService(IDataContext dataContext, IHttpClientFactory httpClientFactory,
      ApplicationConfigurationInfo applicationConfigurationInfo, ICcsSsoEmailService ccsSsoEmailService, IUserProfileHelperService userHelper, IServiceRoleGroupMapperService serviceRoleGroupMapperService)
    {
      _dataContext = dataContext;
      _httpClientFactory = httpClientFactory;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
      _userHelper = userHelper;
      _serviceRoleGroupMapperService = serviceRoleGroupMapperService;
    }

    public async Task<List<ServicePermissionDto>> GetPermissions(string userName, string serviceClientId, string organisationId)
    {
      var users = await _dataContext.User.Include(p => p.Party).ThenInclude(p => p.Person).ThenInclude(o => o.Organisation)
      .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
      .ThenInclude(oug => oug.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
      .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
      .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
      .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
      .Where(u => !u.IsDeleted && u.UserName == userName).ToListAsync();

      User user;
      if (!string.IsNullOrWhiteSpace(organisationId))
      {
        user = users.FirstOrDefault(u => u.Party.Person.Organisation.CiiOrganisationId == organisationId);
      }
      else
      {
        user = users.SingleOrDefault(u => u.UserType == Core.DbModel.Constants.UserType.Primary);
      }

      var rolePermissions = user.UserAccessRoles.Where(uar => !uar.IsDeleted).Select(uar => new UserRolePermissionInfo
      {
        RoleKey = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
        RoleName = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
        PermissionList = uar.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.Where(sp => sp.ServicePermission.CcsService.ServiceClientId == serviceClientId).Select(srp => srp.ServicePermission.ServicePermissionName).ToList()
      }).ToList();

      var userGroupsApprovalRequest = await _dataContext.UserAccessRolePending.Where(x => !x.IsDeleted && x.UserId == user.Id
      && x.OrganisationUserGroupId != null && x.Status == (int)UserPendingRoleStaus.Pending).ToListAsync();
      var serviceRoleGroups = await _serviceRoleGroupMapperService.OrgRolesToServiceRoleGroupsAsync(userGroupsApprovalRequest.Select(x => x.OrganisationEligibleRoleId).ToList());

      if (user.UserGroupMemberships != null)
      {
        GetGroupPermissions(serviceClientId, user, rolePermissions, userGroupsApprovalRequest, serviceRoleGroups);
      }

      var permissions = rolePermissions.SelectMany(rp => rp.PermissionList, (r, p) => new ServicePermissionDto()
      {
        RoleKey = r.RoleKey,
        RoleName = r.RoleName,
        PermissionName = p
      }).Distinct().ToList();

      return permissions;
    }
    public async Task SendUserActivationEmailAsync(string email, bool isExpired = false)
    {
      _userHelper.ValidateUserName(email);

      var client = _httpClientFactory.CreateClient("default");
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      var url = $"security/users/activation-emails?is-expired={isExpired}";
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);

      var list = new List<KeyValuePair<string, string>>();
      list.Add(new KeyValuePair<string, string>("email", email));
      HttpContent codeContent = new FormUrlEncodedContent(list);
      await client.PostAsync(url, codeContent);
    }

    public async Task NominateUserAsync(string email)
    {
      _userHelper.ValidateUserName(email);
      var url = _applicationConfigurationInfo.ConclaveSettings.BaseUrl + _applicationConfigurationInfo.ConclaveSettings.OrgRegistrationRoute;
      await _ccsSsoEmailService.SendNominateEmailAsync(email, url);
    }

    private void GetGroupPermissions(string serviceClientId, User user, List<UserRolePermissionInfo> rolePermissions, List<UserAccessRolePending> userGroupsApprovalRequest, List<Core.DbModel.Entity.CcsServiceRoleGroup> serviceRoleGroups)
    {
      foreach (var userGroupMembership in user.UserGroupMemberships)
      {
        if (!userGroupMembership.IsDeleted && userGroupMembership.OrganisationUserGroup.GroupEligibleRoles != null && userGroupMembership.OrganisationUserGroup.GroupEligibleRoles.Any())
        {
          foreach (var groupAccess in userGroupMembership.OrganisationUserGroup.GroupEligibleRoles.Where(x => !x.IsDeleted))
          {
            if (_applicationConfigurationInfo.UserRoleApproval.Enable && userGroupsApprovalRequest.Any(x => x.OrganisationUserGroupId == groupAccess.OrganisationUserGroupId && serviceRoleGroups.Any(g => g.CcsServiceRoleMappings.Any(m => m.CcsAccessRoleId == groupAccess.OrganisationEligibleRole.CcsAccessRoleId))))
            {
              continue;
            }
            var groupAccessRole = new UserRolePermissionInfo
            {
              RoleName = groupAccess.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
              RoleKey = groupAccess.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
              PermissionList = groupAccess.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.Where(sp => sp.ServicePermission.CcsService.ServiceClientId == serviceClientId).Select(srp => srp.ServicePermission.ServicePermissionName).ToList()
            };
            rolePermissions.Add(groupAccessRole);
          }
        }
      }
    }

  }
}
