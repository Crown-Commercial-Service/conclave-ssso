using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IUserProfileService
  {
    // #Auto validation
    Task<UserEditResponseInfo> CreateUserAsync(UserProfileEditRequestInfo userProfileRequestInfo, bool isNewOrgAdmin = false);

    Task DeleteUserAsync(string userName, bool checkForLastAdmin = true);
    // #Delegated
    Task<UserProfileResponseInfo> GetUserAsync(string userName, bool isDelegated = false, bool isSearchUser = false, string delegatedOrgId = "");
    // #Delegated
    Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, UserFilterCriteria userFilterCriteria);

    Task<UserListWithServiceGroupRoleResponse> GetUsersV1Async(string organisationId, ResultSetCriteria resultSetCriteria, UserFilterCriteria userFilterCriteria);

    Task<AdminUserListResponse> GetAdminUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria);

    Task<UserEditResponseInfo> UpdateUserAsync(string userName, UserProfileEditRequestInfo userProfileRequestInfo);

    Task VerifyUserAccountAsync(string userName);

    Task ResetUserPasswodAsync(string userName, string? component);

    Task RemoveAdminRolesAsync(string userName);

    Task AddAdminRoleAsync(string userName);

    Task<bool> IsUserExist(string userName);

    // #Delegated
    Task CreateDelegatedUserAsync(DelegatedUserProfileRequestInfo userProfileRequestInfo);
    Task CreateDelegatedUserV1Async(DelegatedUserProfileServiceRoleGroupRequestInfo userProfileServiceRoleGroupRequestInfo);

    Task UpdateDelegatedUserAsync(DelegatedUserProfileRequestInfo userProfileRequestInfo);
    Task UpdateDelegatedUserV1Async(DelegatedUserProfileServiceRoleGroupRequestInfo userProfileServiceRoleGroupRequestInfo);

    Task RemoveDelegatedAccessForUserAsync(string userName, string organisationId);

    Task AcceptDelegationAsync(string acceptanceToken);

    Task SendUserDelegatedAccessEmailAsync(string userName, string orgId = "", string orgName = "");

    Task<UserProfileServiceRoleGroupResponseInfo> GetUserV1Async(string userName, bool isDelegated = false, bool isSearchUser = false, string delegatedOrgId = "");

    Task<UserEditResponseInfo> CreateUserV1Async(UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo, bool isNewOrgAdmin = false);

    Task<UserEditResponseInfo> UpdateUserV1Async(string userName, UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo);

    Task<OrganisationJoinRequest> GetUserJoinRequestDetails(string joiningDetailsToken);
  }
}
