using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IWrapperUserService
    {
        Task<UserAccessRolePendingRequestDetails> GetRoleApprovalLinkExpiredData(UserAccessRolePendingFilterCriteria criteria);
        Task<bool> DeleteUserAccessRolePending(List<int> roleIds);
        Task<UserResponse> GetUserByUserId(int UserId);
    }
}
