using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
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
    public ConfigurationDetailService(IDataContext dataContext, ILocalCacheService localCacheService)
    {
      _dataContext = dataContext;
      _localCacheService = localCacheService;
    }
    public async Task<List<IdentityProviderDetail>> GetIdentityProvidersAsync()
    {
      var identityProviders = await _dataContext.IdentityProvider.Select(i => new IdentityProviderDetail
      {
        Id = i.Id,
        ConnectionName = i.IdpConnectionName,
        Name = i.IdpName
      }).ToListAsync();

      return identityProviders;
    }

    public async Task<List<OrganisationRole>> GetRolesAsync()
    {
      var roles = await _dataContext.CcsAccessRole
                          .Include(or => or.ServiceRolePermissions).ThenInclude(sr => sr.ServicePermission).ThenInclude(sr => sr.CcsService)
                          .Select(i => new OrganisationRole
                          {
                            RoleId = i.Id,
                            RoleName = i.CcsAccessRoleName,
                            ServiceName = i.ServiceRolePermissions.FirstOrDefault().ServicePermission.CcsService.ServiceName,
                            OrgTypeEligibility = i.OrgTypeEligibility,
                            SubscriptionTypeEligibility = i.SubscriptionTypeEligibility,
                            TradeEligibility = i.TradeEligibility
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
  }
}
