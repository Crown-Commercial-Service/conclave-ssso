using CcsSso.Core.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
    public interface IConfigurationDetailService
    {
        Task<List<IdentityProviderDetail>> GetIdentityProvidersAsync();

        Task<List<OrganisationRole>> GetRolesAsync();

        Task<List<CcsServiceInfo>> GetCcsServicesAsync();

        Task<ServiceProfile> GetServiceProfieAsync(string clientId, string organisationId);

        Task<int> GetDashboardServiceIdAsync();

        Task<List<CountryDetail>> GetCountryDetailAsync();

        Task<List<OrganisationRole>> GetRolesRequireApprovalAsync();

        Task<List<ServiceRoleGroup>> GetServiceRoleGroupsAsync();

        Task<List<ServiceRoleGroup>> GetServiceRoleGroupsRequireApprovalAsync();
  }
}
