using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  public class SecurityService : ISecurityService
  {

    private readonly IIdentityProviderService _identityProviderService;
    public SecurityService(IIdentityProviderService awsIdentityProviderService)
    {
      _identityProviderService = awsIdentityProviderService;
    }

    public async Task<AuthResultDto> LoginAsync(string userName, string userPassword)
    {
      var result = await _identityProviderService.AuthenticateAsync(userName, userPassword);
      return result;
    }

    public async Task<string> GetRenewedTokenAsync(string refreshToken)
    {
      if (string.IsNullOrEmpty(refreshToken))
      {
        throw new CcsSsoException("INVALID_REFRESH_TOKEN");
      }

      var idToken = await _identityProviderService.GetRenewedTokenAsync(refreshToken);
      return idToken;
    }

    public async Task<List<IdentityProviderInfoDto>> GetIdentityProvidersListAsync()
    {
      var idProviders = await _identityProviderService.ListIdentityProvidersAsync();
      return idProviders;
    }

    public async Task ChangePasswordAsync(ChangePasswordDto changePassword)
    {
      if(string.IsNullOrEmpty(changePassword.AccessToken))
      {
        throw new CcsSsoException("ACCESS_TOKEN_REQUIRED");
      }
      if (string.IsNullOrEmpty(changePassword.NewPassword))
      {
        throw new CcsSsoException("NEW_PASSWORD_REQUIRED");
      }
      if (string.IsNullOrEmpty(changePassword.OldPassword))
      {
        throw new CcsSsoException("OLD_PASSWORD_REQUIRED");
      }
      await _identityProviderService.ChangePasswordAsync(changePassword);
    }

    public async Task InitiateResetPasswordAsync(string userName)
    {
      if (string.IsNullOrEmpty(userName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }
      await _identityProviderService.InitiateResetPasswordAsync(userName);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto resetPassword)
    {
      if (string.IsNullOrEmpty(resetPassword.VerificationCode))
      {
        throw new CcsSsoException("VERIFICATION_CODE_REQUIRED");
      }
      if (string.IsNullOrEmpty(resetPassword.UserName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }
      if (string.IsNullOrEmpty(resetPassword.NewPassword))
      {
        throw new CcsSsoException("NEW_PASSWORD_REQUIRED");
      }
      await _identityProviderService.ResetPasswordAsync(resetPassword);
    }

    public async Task LogoutAsync(string userName)
    {
      if (string.IsNullOrEmpty(userName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }
      await _identityProviderService.SignOutAsync(userName);
    }
  }
}
