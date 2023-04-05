using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class ConfigurationDetailService : IConfigurationDetailService
  {
    private readonly IDataContext _dataContext;
    private ILocalCacheService _localCacheService;
    private ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IServiceRoleGroupMapperService _rolesToServiceRoleGroupMapperService;
   
    public ConfigurationDetailService(IDataContext dataContext, ILocalCacheService localCacheService, ApplicationConfigurationInfo applicationConfigurationInfo,
      IServiceRoleGroupMapperService rolesToServiceRoleGroupMapperService)
    {
      _dataContext = dataContext;
      _localCacheService = localCacheService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _rolesToServiceRoleGroupMapperService = rolesToServiceRoleGroupMapperService;
    }
    public async Task<List<IdentityProviderDetail>> GetIdentityProvidersAsync()
    {
      var identityProviders = await _dataContext.IdentityProvider.OrderBy(o => o.DisplayOrder).Select(i => new IdentityProviderDetail
      {
        Id = i.Id,
        ConnectionName = i.IdpConnectionName,
        Name = i.IdpName
      }).ToListAsync();

      return identityProviders;
    }

    public async Task<List<OrganisationRole>> GetRolesAsync()
    {
      var autoValidationRolesForOrg = await _dataContext.AutoValidationRole.Where(x => x.AssignToOrg == true).ToListAsync();
      var roles = await _dataContext.CcsAccessRole
                          .Where(r => !r.IsDeleted)
                          .Include(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                          .Select(i => new OrganisationRole
                          {
                            RoleId = i.Id,
                            RoleName = i.CcsAccessRoleName,
                            RoleKey = i.CcsAccessRoleNameKey,
                            ServiceName = i.ServiceRolePermissions.FirstOrDefault().ServicePermission.CcsService.ServiceName,
                            OrgTypeEligibility = i.OrgTypeEligibility,
                            SubscriptionTypeEligibility = i.SubscriptionTypeEligibility,
                            TradeEligibility = i.TradeEligibility,
                            AutoValidationRoleTypeEligibility = _applicationConfigurationInfo.OrgAutoValidation.Enable ? GetAutoValidationTypeEligibility(i.Id, autoValidationRolesForOrg) : default
                          }).ToListAsync();

      return roles;
    }

    public async Task<List<CcsServiceInfo>> GetCcsServicesAsync()
    {
      var ccsServices = _localCacheService.GetValue<List<CcsServiceInfo>>(CacheKeys.CcsServices);
      if (ccsServices == null || ccsServices.Count == 0)
      {
        ccsServices = await _dataContext.CcsService.Where(c => c.IsDeleted == false).
          Select(c => new CcsServiceInfo()
          {
            Id = c.Id,
            Name = c.ServiceName,
            Url = c.ServiceUrl,
            Code = c.ServiceCode,
            Description = c.Description
          }).ToListAsync();

        _localCacheService.SetValue(CacheKeys.CcsServices, ccsServices, new TimeSpan(0, 0, 10));
      }
      return ccsServices;
    }

    public async Task<ServiceProfile> GetServiceProfieAsync(string clientId, string organisationId)
    {
      var service = await _dataContext.CcsService.Where(s => s.ServiceClientId == clientId)
       .Include(s => s.ExternalServiceRoleMappings).ThenInclude(exrm => exrm.OrganisationEligibleRole).ThenInclude(oer => oer.Organisation)
       .Include(s => s.ExternalServiceRoleMappings).ThenInclude(exrm => exrm.OrganisationEligibleRole).ThenInclude(oer => oer.CcsAccessRole)
       .FirstOrDefaultAsync();

      if (service == null)
      {
        throw new ResourceNotFoundException();
      }

      ServiceProfile serviceProfile = new();

      if (service.GlobalLevelOrganisationAccess)
      {
        serviceProfile = new ServiceProfile
        {
          Audience = _applicationConfigurationInfo.DashboardServiceClientId,
          ServiceId = service.Id,
          RoleKeys = _applicationConfigurationInfo.ServiceDefaultRoleInfo.GlobalServiceDefaultRoles
        };
      }
      else
      {
        serviceProfile = new ServiceProfile
        {
          Audience = _applicationConfigurationInfo.DashboardServiceClientId,
          ServiceId = service.Id,
          RoleKeys = service.ExternalServiceRoleMappings.Where(exrm => exrm.OrganisationEligibleRole.Organisation.CiiOrganisationId == organisationId)
            .Select(exrm => exrm.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey).ToList()
        };
      }

      return serviceProfile;
    }

    public async Task<int> GetDashboardServiceIdAsync()
    {
      var serviceId = await _localCacheService.GetOrSetValueAsync<int>(CacheKeys.DashboardServiceId, async () => (await _dataContext.CcsService
        .FirstOrDefaultAsync(s => s.ServiceClientId == _applicationConfigurationInfo.DashboardServiceClientId)).Id);
      return serviceId;
    }

    public async Task<List<CountryDetail>> GetCountryDetailAsync()
    {
      var countryDetail = await _dataContext.CountryDetails.Select(i => new CountryDetail
      {
        Id = i.Id,
        CountryCode = i.Code,
        CountryName = i.Name
      }).OrderBy(a => a.CountryName).ToListAsync();

      return countryDetail;
    }

    // #Auto validation
    private static int[] GetAutoValidationTypeEligibility(int CcsAccessRoleId, List<AutoValidationRole> autoValidationRoles)
    {
      var role = autoValidationRoles.FirstOrDefault(x => x.CcsAccessRoleId == CcsAccessRoleId);
      List<int> eligibleTradeTypeArrayList = new List<int>();
      if (role != null)
      {
        if (role.IsBothSuccess || role.IsBothFailed)
        {
          eligibleTradeTypeArrayList.Add((int)RoleEligibleTradeType.Both);
        }
        if (role.IsSupplier)
        {
          eligibleTradeTypeArrayList.Add((int)RoleEligibleTradeType.Supplier);
        }
        if (role.IsBuyerSuccess || role.IsBuyerFailed)
        {
          eligibleTradeTypeArrayList.Add((int)RoleEligibleTradeType.Buyer);
        }
      }
      return eligibleTradeTypeArrayList.ToArray();
    }

    public async Task<List<OrganisationRole>> GetRolesRequireApprovalAsync()
    {
      if (!_applicationConfigurationInfo.UserRoleApproval.Enable)
      {
        throw new InvalidOperationException();
      }

      var roles = await _dataContext.CcsAccessRole
                          .Where(r => !r.IsDeleted && r.ApprovalRequired == (int)RoleApprovalRequiredStatus.ApprovalRequired)
                          .Include(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                          .Select(i => new OrganisationRole
                          {
                            RoleId = i.Id,
                            RoleName = i.CcsAccessRoleName,
                            RoleKey = i.CcsAccessRoleNameKey,
                            ServiceName = i.ServiceRolePermissions.FirstOrDefault().ServicePermission.CcsService.ServiceName,
                            OrgTypeEligibility = i.OrgTypeEligibility,
                            SubscriptionTypeEligibility = i.SubscriptionTypeEligibility,
                            TradeEligibility = i.TradeEligibility,
                          }).ToListAsync();

      return roles;
    }

    #region Service Role Group

    public async Task<List<ServiceRoleGroup>> GetServiceRoleGroupsRequireApprovalAsync()
    {
      if (!_applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var roles = await GetRolesRequireApprovalAsync();
      var serviceRoleGroupsEntity = await _rolesToServiceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(roles.Select(x => x.RoleId).ToList());
      var serviceRoleGroups = serviceRoleGroupsEntity.Select(x => new ServiceRoleGroup
      {
        Id = x.Id,
        Key = x.Key,
        Name = x.Name,
        OrgTypeEligibility = x.OrgTypeEligibility,
        SubscriptionTypeEligibility = x.SubscriptionTypeEligibility,
        TradeEligibility = x.TradeEligibility,
        DisplayOrder = x.DisplayOrder,
        Description = x.Description
      }).ToList();

      return serviceRoleGroups;
    }


    public async Task<List<ServiceRoleGroup>> GetServiceRoleGroupsAsync()
    {
      if (!_applicationConfigurationInfo.ServiceRoleGroupSettings.Enable)
      {
        throw new InvalidOperationException();
      }

      var roles = await GetRolesAsync();
      var serviceRoleGroupsEntity = await _rolesToServiceRoleGroupMapperService.CcsRolesToServiceRoleGroupsAsync(roles.Select(x => x.RoleId).ToList());
      var serviceRoleGroups = serviceRoleGroupsEntity.Select(x => new ServiceRoleGroup
      {
        Id = x.Id,
        Key = x.Key,
        Name = x.Name,
        OrgTypeEligibility = x.OrgTypeEligibility,
        SubscriptionTypeEligibility = x.SubscriptionTypeEligibility,
        TradeEligibility = x.TradeEligibility,
        DisplayOrder = x.DisplayOrder,
        Description = x.Description,
        AutoValidationRoleTypeEligibility = GetServiceAutoValidationElegiblity(x, roles)
      }).ToList();

      return serviceRoleGroups;
    }

    private static int[] GetServiceAutoValidationElegiblity(CcsServiceRoleGroup group, List<OrganisationRole> roles) 
    {
      var groupCcsAccessRoleIds = group.CcsServiceRoleMappings.Select(x => x.CcsAccessRoleId).ToArray();
      var autoValidationEligibilityOfRoles = roles.Where(x => groupCcsAccessRoleIds.Contains(x.RoleId)).Select(x => x.AutoValidationRoleTypeEligibility).ToList();
      List<int> autoValidationRoleTypeEligibilityOfService = new();
      
      foreach (var eligRole in autoValidationEligibilityOfRoles) 
      {
        autoValidationRoleTypeEligibilityOfService.AddRange(eligRole);
      }

      return autoValidationRoleTypeEligibilityOfService.Distinct().ToArray();
    }
    #endregion

  }
}
