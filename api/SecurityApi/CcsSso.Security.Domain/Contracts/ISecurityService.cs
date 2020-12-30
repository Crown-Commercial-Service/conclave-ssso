using CcsSso.Security.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface ISecurityService
  {
    Task<AuthResultDto> LoginAsync(string userName, string userPassword);

    Task<string> GetRenewedTokenAsync(string refreshToken);

    Task<List<IdentityProviderInfoDto>> GetIdentityProvidersListAsync();

    Task ChangePasswordAsync(ChangePasswordDto changePassword);

    Task InitiateResetPasswordAsync(string userName);

    Task ResetPasswordAsync(ResetPasswordDto resetPassword);

    Task LogoutAsync(string userName);
  }
}
