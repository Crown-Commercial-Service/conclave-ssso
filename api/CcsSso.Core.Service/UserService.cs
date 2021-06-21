using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
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
    private readonly IAdaptorNotificationService _adapterNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    public UserService(IDataContext dataContext, IHttpClientFactory httpClientFactory, ApplicationConfigurationInfo applicationConfigurationInfo,
      IAdaptorNotificationService adapterNotificationService, IWrapperCacheService wrapperCacheService)
    {
      _dataContext = dataContext;
      _httpClientFactory = httpClientFactory;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _adapterNotificationService = adapterNotificationService;
      _wrapperCacheService = wrapperCacheService;
    }

    public async Task<UserDetails> GetAsync(int id)
    {
      var user = await _dataContext.User.FirstOrDefaultAsync(u => u.Id == id);
      if (user != null)
      {
        UserDetails userProfileDetails = new UserDetails()
        {
          Id = user.Id,
          // FirstName = "",
          // LastName = ""
          UserName = user.UserName,
        };
        return userProfileDetails;
      }
      throw new ResourceNotFoundException();
    }

    public async Task<UserDetails> GetAsync(string userName)
    {
      var user = await _dataContext.User.Where(u => !u.IsDeleted).Include(u => u.UserGroupMemberships).ThenInclude(c => c.OrganisationUserGroup).FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower());
      if (user != null)
      {
        UserDetails userProfileDetails = new UserDetails()
        {
          Id = user.Id,
          // FirstName = "",
          // LastName = "",
          UserName = user.UserName,
          UserGroups = user.UserGroupMemberships?.Select(ug => new UserGroup()
          {
            Role = user.JobTitle, // This is a temporary implementation
            Group = ug.OrganisationUserGroup?.UserGroupName
          }).ToList()
        };
        return userProfileDetails;
      }
      throw new ResourceNotFoundException();
    }

    /// <summary>
    /// Creates a user
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> CreateAsync(UserDto model)
    {
      // var identifyProvider = _dataContext.IdentityProvider.FirstOrDefault(x => x.IdpConnectionName == "Username-Password-Authentication");
      var identifyProvider = _dataContext.OrganisationEligibleIdentityProvider.FirstOrDefault(x =>
        x.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName && x.OrganisationId == model.OrganisationId
      );
      // var orgGroup = _dataContext.OrganisationUserGroup.FirstOrDefault(x => x.UserGroupNameKey == "ORG_ADMINISTRATOR_GROUP" && x.OrganisationId == model.OrganisationId);
      //var userExists = await _dataContext.User.SingleOrDefaultAsync(t => t.UserName == model.UserName);
      //if (userExists != null)
      //{
      //  throw new System.Exception("UserName already exists");
      //}
      //var identityProviderId = (await _dataContext.IdentityProvider.SingleOrDefaultAsync(t => t.IdpName == "AWS Cognito")).Id;
      var partyType = await _dataContext.PartyType.SingleOrDefaultAsync(t => t.PartyTypeName == "USER");
      var party = new CcsSso.DbModel.Entity.Party
      {
        PartyTypeId = partyType.Id,
        CreatedUserId = 0,
        LastUpdatedUserId = 0,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
        //ContactPoints = new System.Collections.Generic.List<CcsSso.DbModel.Entity.ContactPoint> {
        //  new CcsSso.DbModel.Entity.ContactPoint {
        //    ContactDetail = new CcsSso.DbModel.Entity.ContactDetail
        //    {
        //      EffectiveFrom = System.DateTime.UtcNow
        //    }
        //  }
        //}
      };
      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();
      // return party.Id.ToString();
      var person = new CcsSso.DbModel.Entity.Person
      {
        Title = 1,
        FirstName = model.FirstName,
        LastName = model.LastName,
        OrganisationId = model.OrganisationId,// (model.OrganisationId.HasValue ? model.OrganisationId.Value : 0),
        PartyId = party.Id,
        CreatedUserId = party.Id,
        LastUpdatedUserId = party.Id,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.Person.Add(person);
      await _dataContext.SaveChangesAsync();
      var user = new CcsSso.DbModel.Entity.User
      {
        JobTitle = model.JobTitle,
        UserTitle = 1,
        UserName = model.UserName.ToLower(),
        OrganisationEligibleIdentityProviderId = identifyProvider.Id,
        PartyId = party.Id,
        // PersonId = person.Id,
        CreatedUserId = party.Id,
        LastUpdatedUserId = party.Id,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.User.Add(user);
      var role = await _dataContext.OrganisationEligibleRole.FirstOrDefaultAsync(x => x.CcsAccessRole.CcsAccessRoleNameKey == "ORG_ADMINISTRATOR" && x.OrganisationId == model.OrganisationId);
      var userAccessRole = new CcsSso.DbModel.Entity.UserAccessRole
      {
        User = user,
        OrganisationEligibleRole = role
      };
      _dataContext.UserAccessRole.Add(userAccessRole);

      await _dataContext.SaveChangesAsync();

      // Notify the adapter
      var organisation = await _dataContext.Organisation.FirstAsync(o => o.Id == model.OrganisationId);
      await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Create, model.UserName.ToLower(), organisation.CiiOrganisationId);

      return user.Id.ToString();
      //var server = new CcsSso.DbModel.Entity.IdentityProvider
      //{
      //  IdpName = "name goes here",
      //  IdpUri = "google",
      //  CreatedPartyId = 1,
      //  LastUpdatedPartyId = 1,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.IdentityProvider.Add(server);
      //await _dataContext.SaveChangesAsync();
      //return server.Id.ToString();
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

    public async Task SendUserActivationEmailAsync(string email)
    {
      var client = _httpClientFactory.CreateClient("default");
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      var url = "security/useractivationemail";
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);

      var list = new List<KeyValuePair<string, string>>();
      list.Add(new KeyValuePair<string, string>("email", email));
      HttpContent codeContent = new FormUrlEncodedContent(list);
      await client.PostAsync(url, codeContent);
    }

    /// <summary>
    /// Delete a user by his/her id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(int id)
    {
      var user = await _dataContext.User
        .Where(x => x.Id == id)
        .Include(c => c.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
        .Include(c => c.UserAccessRoles)
        .SingleOrDefaultAsync();
      if (user != null)
      {
        user.IsDeleted = true;
        if (user.Party != null)
        {
          user.Party.IsDeleted = true;
        }
        if (user.UserAccessRoles != null)
        {
          if (user.UserAccessRoles != null && user.UserAccessRoles.Any())
          {
            user.UserAccessRoles.ForEach((e) =>
            {
              e.IsDeleted = true;
            });
          }
        }
        await _dataContext.SaveChangesAsync();

        //Invalidate redis
        await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.User}-{user.UserName}");

        // Notify the adapter
        await _adapterNotificationService.NotifyUserChangeAsync(OperationType.Delete, user.UserName, user.Party.Person.Organisation.CiiOrganisationId);
      }
    }
  }
}
