using CcsSso.Security.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface IIdentityProviderService
  {
    Task<AuthResultDto> AuthenticateAsync(string clientId, string secret, string userName, string userPassword);

    Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo);

    Task UpdateUserAsync(UserInfo userInfo);

    Task UpdateUserMfaFlagAsync(Domain.Dtos.UserInfo userInfo);

    Task UpdatePendingMFAVerifiedFlagAsync(string userName, bool mfaResetVerified);

    Task ResetMfaAsync(string userName);

    Task<TokenResponseInfo> GetRenewedTokensAsync(string clientId, string clientSecret, string refreshToken, string sid);

    Task<TokenResponseInfo> GetMachineTokenAsync(string clientId, string clientSecret, string audience);

    Task<TokenResponseInfo> GetTokensAsync(TokenRequestInfo tokenRequestInfo, string sid);

    Task RevokeTokenAsync(string refreshToken);
      
    Task<List<IdentityProviderInfoDto>> ListIdentityProvidersAsync();

    Task ChangePasswordAsync(ChangePasswordDto changePassword);

    Task<AuthResultDto> RespondToNewPasswordRequiredAsync(PasswordChallengeDto passwordChallengeDto);

    Task InitiateResetPasswordAsync(ChangePasswordInitiateRequest changePasswordInitiateRequest);

    Task<string> SignOutAsync(string clientId, string userName);

    Task DeleteAsync(string email);

    Task<IdamUser> GetIdamUserAsync(string email);

    Task<string> GetIdentityProviderAuthenticationEndPointAsync();

    string GetSAMLAuthenticationEndPoint(string clientId);

    string GetAuthenticationEndPoint(string state, string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt, string nonce, string display, string login_hint, int? max_age, string acr_values);

    Task SendUserActivationEmailAsync(string email, string managementApiToken = null, bool isExpired = false);
  }
}
