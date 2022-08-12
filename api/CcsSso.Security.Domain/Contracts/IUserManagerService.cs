using CcsSso.Security.Domain.Dtos;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface IUserManagerService
  {
    Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo);
    
    Task UpdateUserAsync(UserInfo userInfo);

    Task UpdateUserMfaFlagAsync(UserInfo userInfo);

    Task ResetMfaAsync(string ticket, string userName);

    Task SendResetMfaNotificationAsync(MfaResetRequest mfaResetRequest);

    Task DeleteUserAsync(string email);

    Task<IdamUserInfo> GetUserAsync();

    Task<IdamUser> GetUserAsync(string email);

    Task SendUserActivationEmailAsync(string email, bool isExpired = false);
    Task<string> GetActivationEmailVerificationLink(string email);
  }
}
