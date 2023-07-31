using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Domain.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace CcsSso.Core.Service.Wrapper
{
    public class WrapperConfigurationService : IWrapperConfigurationService
    {

        private readonly IWrapperApiService _wrapperApiService;
        public WrapperConfigurationService(IWrapperApiService wrapperApiService)
        {
            _wrapperApiService = wrapperApiService;
        }
        public async Task<List<RoleApprovalConfigurationInfo>> GetRoleApprovalConfigurationsAsync()
        {
            var result = await _wrapperApiService.GetAsync<List<RoleApprovalConfigurationInfo>>(WrapperApi.Configuration, $"internal/approve/roles/config", $"{CacheKeyConstant.Configuration}-ROLE_APPROVAL_CONFIGURATION", "ERROR_RETRIEVING_ROLES_APPROVAL_CONFIGURATION");
            return result;
        }

        public async Task<OrganisationProfileResponseInfo> GetOrganisationDetailsById(int organisationId)
        {
            var result = await _wrapperApiService.GetAsync<OrganisationProfileResponseInfo>(WrapperApi.Organisation, $"internal/{organisationId}", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION");
            return result;
        }

        public async Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId)
        {
            var result = await _wrapperApiService.GetAsync<List<OrganisationRole>>(WrapperApi.Organisation, $"{organisationId}/roles", $"{CacheKeyConstant.Organisation}-{organisationId}-ROLES", "ORGANISATION_ROLES_NOT_FOUND");
            return result;
        }

        public async Task<List<ServiceRoleGroup>> GetServiceRoleGroupsRequireApproval()
        {
            var result = await _wrapperApiService.GetAsync<List<ServiceRoleGroup>>(WrapperApi.Configuration, $"approve/servicerolegroups", $"{CacheKeyConstant.Configuration}-ROLES", "ERROR_RETRIEVING_SERVICE_ROLE_GROUPS_REQUIRE_APPROVAL");
            return result;
        }
    }
}