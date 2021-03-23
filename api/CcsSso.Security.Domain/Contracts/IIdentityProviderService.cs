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

    Task<TokenResponseInfo> GetRenewedTokensAsync(string clientId, string refreshToken);

    Task<TokenResponseInfo> GetTokensAsync(TokenRequestInfo tokenRequestInfo, string sid);

    Task RevokeTokenAsync(string refreshToken);

    Task<List<IdentityProviderInfoDto>> ListIdentityProvidersAsync();

    Task ChangePasswordAsync(ChangePasswordDto changePassword);

    Task<AuthResultDto> RespondToNewPasswordRequiredAsync(PasswordChallengeDto passwordChallengeDto);

    Task InitiateResetPasswordAsync(string userName);

    Task ResetPasswordAsync(ResetPasswordDto resetPassword);

    Task<string> SignOutAsync(string clientId, string userName);

    Task<UserClaims> GetUserAsync(string accessToken);

    Task DeleteAsync(string email);

    Task<string> GetIdentityProviderAuthenticationEndPointAsync();

    string GetAuthenticationEndPoint(string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt);
  }
}
