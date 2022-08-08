using CcsSso.Core.Domain.Dtos.External;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
    public interface IUserProfileService
    {
        Task<UserEditResponseInfo> CreateUserAsync(UserProfileEditRequestInfo userProfileRequestInfo);

        Task DeleteUserAsync(string userName, bool checkForLastAdmin = true);

        Task<UserProfileResponseInfo> GetUserAsync(string userName);

        Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, string searchString = null, bool includeSelf = false, bool isDelegatedOnly = false, bool isDelegatedExpiredOnly = false);

        Task<AdminUserListResponse> GetAdminUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria);

        Task<UserEditResponseInfo> UpdateUserAsync(string userName, UserProfileEditRequestInfo userProfileRequestInfo);

        Task VerifyUserAccountAsync(string userName);

        Task ResetUserPasswodAsync(string userName, string? component);

        Task RemoveAdminRolesAsync(string userName);

        Task AddAdminRoleAsync(string userName);

        Task CreateDelegatedUserAsync(DelegatedUserProfileRequestInfo userProfileRequestInfo);

        Task UpdateDelegatedUserAsync(DelegatedUserProfileRequestInfo userProfileRequestInfo);
        
        Task RemoveDelegatedAccessForUserAsync(string userName, string organisationId);
    }
}
