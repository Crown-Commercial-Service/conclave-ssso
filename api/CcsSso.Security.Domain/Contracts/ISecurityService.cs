using CcsSso.Security.Domain.Dtos;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface ISecurityService
  {
    Task<AuthResultDto> LoginAsync(string clientId, string secret, string userName, string userPassword);

    string GetAuthenticationEndPoint(string sid, string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt);

    Task<TokenResponseInfo> GetRenewedTokenAsync(TokenRequestInfo tokenRequestInfo, string opbsValue, string host, string sid);

    Task<List<IdentityProviderInfoDto>> GetIdentityProvidersListAsync();

    Task ChangePasswordAsync(ChangePasswordDto changePassword);

    Task<AuthResultDto> ChangePasswordWhenPasswordChallengeAsync(PasswordChallengeDto passwordChallengeDto);

    Task InitiateResetPasswordAsync(string userName);

    Task ResetPasswordAsync(ResetPasswordDto resetPassword);

    Task<string> LogoutAsync(string clientId, string redirecturi);

    Task<List<string>> PerformBackChannelLogoutAsync(string clientId, string sid, List<string> relyingParties);

    Task<string> GetIdentityProviderAuthenticationEndPointAsync();

    Task RevokeTokenAsync(string refreshToken);

    JsonWebKeySet GetJsonWebKeyTokens();

    bool ValidateToken(string clientId, string token);

  }
}
