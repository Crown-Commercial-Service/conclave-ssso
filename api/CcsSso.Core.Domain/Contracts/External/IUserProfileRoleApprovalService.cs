using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IUserProfileRoleApprovalService
  {
    Task<bool> UpdateUserRoleStatusAsync(UserRoleApprovalEditRequest userApprovalRequest);

    Task<List<UserAccessRolePendingDetails>> GetUserRolesPendingForApprovalAsync(string userName);

    Task<UserAccessRolePendingTokenDetails> VerifyAndReturnRoleApprovalTokenDetailsAsync(string token);

    Task RemoveApprovalPendingRolesAsync(string userName, string roles);

    Task CreateUserRolesPendingForApprovalAsync(UserProfileEditRequestInfo userProfileRequestInfo, bool sendEmailNotification = true);

    Task<List<UserServiceRoleGroupPendingDetails>> GetUserServiceRoleGroupsPendingForApprovalAsync(string userName);

    Task<UserAccessServiceRoleGroupPendingTokenDetails> VerifyAndReturnServiceRoleGroupApprovalTokenDetailsAsync(string token);
  }
}
