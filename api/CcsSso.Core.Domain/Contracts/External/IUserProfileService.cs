using CcsSso.Core.Domain.Dtos.External;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IUserProfileService
  {
    Task<UserEditResponseInfo> CreateUserAsync(UserProfileEditRequestInfo userProfileRequestInfo);

    Task DeleteUserAsync(string userName, bool checkForLastAdmin = true);

    Task<UserProfileResponseInfo> GetUserAsync(string userName);

    Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, string searchString = null, bool includeSelf = false);

    Task<UserEditResponseInfo> UpdateUserAsync(string userName, UserProfileEditRequestInfo userProfileRequestInfo);

    Task ResetUserPasswodAsync(string userName, string? component);

    Task RemoveAdminRolesAsync(string userName);

    Task AddAdminRoleAsync(string userName);
  }
}
