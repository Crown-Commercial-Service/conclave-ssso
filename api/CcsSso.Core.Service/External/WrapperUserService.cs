using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Domain.Constants;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class WrapperUserService : IWrapperUserService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperUserService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }
    public async Task<UserAccessRolePendingRequestDetails> GetRoleApprovalLinkExpiredData(UserAccessRolePendingFilterCriteria criteria)
    {
      var payload = JsonConvert.SerializeObject(criteria);
      return await _wrapperApiService.GetAsync<UserAccessRolePendingRequestDetails>(WrapperApi.User, $"internal/approve/roles/{payload}", $"{CacheKeyConstant.User}-USER_ACCESSROLE_PENDING", "ERROR_RETRIEVING_USER_ACCESSROLE_PENDING");
    }

    public async Task<UserResponse> GetUserByUserId(int UserId)
    {
      return await _wrapperApiService.GetAsync<UserResponse>(WrapperApi.User, $"internal/user/{UserId}", $"{CacheKeyConstant.User}-{UserId}", "ERROR_RETRIEVING_USER");
    }

    public async Task<bool> DeleteUserAccessRolePending(List<int> roleIds)
    {
      var Ids = string.Join(",", roleIds);
      return await _wrapperApiService.DeleteAsync<bool>(WrapperApi.User, $"internal/approve/roles?roleIds={Ids}", "ERROR_DELETING_USER_ACCESS_ROLE_PENDING");
    }
  }
}
