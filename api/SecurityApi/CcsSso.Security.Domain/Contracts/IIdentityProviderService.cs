using CcsSso.Security.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface IIdentityProviderService
  {
    Task<AuthResultDto> AuthenticateAsync(string userName, string userPassword);

    Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo);

    Task UpdateUserAsync(UserInfo userInfo);

    Task<string> GetRenewedTokenAsync(string refreshToken);

    Task<List<IdentityProviderInfoDto>> ListIdentityProvidersAsync();

    Task ChangePasswordAsync(ChangePasswordDto changePassword);

    Task InitiateResetPasswordAsync(string userName);

    Task ResetPasswordAsync(ResetPasswordDto resetPassword);

    Task SignOutAsync(string userName);
  }
}
