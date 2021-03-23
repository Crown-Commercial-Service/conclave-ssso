using CcsSso.Core.Domain.Dtos.External;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IUserProfileService
  {
    Task<UserProfileResponseInfo> GetUserAsync(string userName);

    Task<UserListResponse> GetUsersAsync(string organisationId, ResultSetCriteria resultSetCriteria, string userName = null);

    Task UpdateUserAsync(string userName, bool isMyProfile, UserProfileRequestInfo userProfileRequestInfo);

    Task<string> CreateUserAsync(UserProfileRequestInfo userProfileRequestInfo);
  }
}
