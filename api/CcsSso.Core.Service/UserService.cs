using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
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

    public UserService(IDataContext dataContext, IHttpClientFactory httpClientFactory,
      ApplicationConfigurationInfo applicationConfigurationInfo, ICcsSsoEmailService ccsSsoEmailService, IUserProfileHelperService userHelper)
    {
      _dataContext = dataContext;
      _httpClientFactory = httpClientFactory;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
      _userHelper = userHelper;
    }

    public async Task<List<ServicePermissionDto>> GetPermissions(string userName, string serviceClientId)
    {
      var user = await _dataContext.User
      .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
      .ThenInclude(oug => oug.GroupEligibleRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
      .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
      .Include(u => u.UserAccessRoles).ThenInclude(gr => gr.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
      .ThenInclude(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
      .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

      var rolePermissions = user.UserAccessRoles.Where(uar => !uar.IsDeleted).Select(uar => new UserRolePermissionInfo
      {
        RoleKey = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey,
        RoleName = uar.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleName,
        PermissionList = uar.OrganisationEligibleRole.CcsAccessRole.ServiceRolePermissions.Where(sp => sp.ServicePermission.CcsService.ServiceClientId == serviceClientId).Select(srp => srp.ServicePermission.ServicePermissionName).ToList()
      }).ToList();

      if (user.UserGroupMemberships != null)
      {
        foreach (var userGroupMembership in user.UserGroupMemberships)
        {
          if (!userGroupMembership.IsDeleted && userGroupMembership.OrganisationUserGroup.GroupEligibleRoles != null)
          {

            if (userGroupMembership.OrganisationUserGroup.GroupEligibleRoles.Any())
            {
              foreach (var groupAccess in userGroupMembership.OrganisationUserGroup.GroupEligibleRoles.Where(x => !x.IsDeleted))
              {
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
      var url = _applicationConfigurationInfo.ConclaveSettings.BaseUrl + _applicationConfigurationInfo.ConclaveSettings.OrgRegistrationRoute;
      await _ccsSsoEmailService.SendNominateEmailAsync(email, url);
    }
  }
}
