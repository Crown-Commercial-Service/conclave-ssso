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
    public ConfigurationDetailService(IDataContext dataContext, ILocalCacheService localCacheService, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _dataContext = dataContext;
      _localCacheService = localCacheService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
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
  }
}
