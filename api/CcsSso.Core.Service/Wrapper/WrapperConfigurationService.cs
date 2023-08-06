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
      var result = await _wrapperApiService.GetAsync<List<RoleApprovalConfigurationInfo>>(WrapperApi.Configuration, $"approval/roles/config", $"{CacheKeyConstant.Configuration}-ROLE_APPROVAL_CONFIGURATION", "ERROR_RETRIEVING_ROLES_APPROVAL_CONFIGURATION");
      return result;
    }

    public async Task<List<ServiceRoleGroup>> GetServiceRoleGroupsRequireApproval()
    {
      var result = await _wrapperApiService.GetAsync<List<ServiceRoleGroup>>(WrapperApi.Configuration, $"approve/servicerolegroups", $"{CacheKeyConstant.Configuration}-ROLES", "ERROR_RETRIEVING_SERVICE_ROLE_GROUPS_REQUIRE_APPROVAL");
      return result;
    }
  }
}