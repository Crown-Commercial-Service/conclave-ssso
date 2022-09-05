using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Security.Services
{
  public class MockIdentityProviderService : IIdentityProviderService
  {
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJwtTokenHandler _jwtTokenHandler;

    public MockIdentityProviderService(ApplicationConfigurationInfo appConfigInfo, IHttpClientFactory httpClientFactory, IJwtTokenHandler jwtTokenHandler)
    {
      _appConfigInfo = appConfigInfo;
      _httpClientFactory = httpClientFactory;
      _jwtTokenHandler = jwtTokenHandler;
    }

    public async Task<AuthResultDto> AuthenticateAsync(string clientId, string secret, string userName, string userPassword)
    {
      await Task.CompletedTask;
      return new AuthResultDto();
    }

    public async Task ChangePasswordAsync(ChangePasswordDto changePassword)
    {
      await Task.CompletedTask;
    }

    public async Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo)
    {
      await Task.CompletedTask;
      return new UserRegisterResult
      {
        UserName = userInfo.UserName
      };
    }

    public async Task DeleteAsync(string email)
    {
      await Task.CompletedTask;
    }

    public string GetAuthenticationEndPoint(string state, string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt, string nonce, string display, string login_hint, int? max_age, string acr_values)
    {
      string uri = $"{_appConfigInfo.MockProvider.LoginUrl}?client_id={client_id}" +
            $"&response_type={response_type}&scope={scope}&redirect_uri={redirect_uri}&state={state}";
      return uri;
    }

    public async Task<IdamUserInfo> GetIdamUserInfoAsync(string email)
    {
      await Task.CompletedTask;
      return new IdamUserInfo();
    }

    public async Task<IdamUser> GetIdamUserByEmailAsync(string email)
    {
      await Task.CompletedTask;
      return new IdamUser();
    }

    public async Task<string> GetIdentityProviderAuthenticationEndPointAsync()
    {
      await Task.CompletedTask;
      List<string> scopes = new List<string>() { "openid", "offline_access" };

      var url = $"{_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl}/authorize?client_id={_appConfigInfo.Auth0ConfigurationInfo.ClientId}&response_type=code" +
                      $"&scope={string.Join("+", scopes)}&redirect_uri={"client.Callbacks.First()"}";
      return url;
    }

    public async Task<TokenResponseInfo> GetMachineTokenAsync(string clientId, string clientSecret, string audience)
    {
      await Task.CompletedTask;
      return new TokenResponseInfo();
    }

    public async Task<TokenResponseInfo> GetRenewedTokensAsync(TokenRequestInfo tokenRequestInfo, string sid)
    {
      var userDetails = await GetUserAsync(tokenRequestInfo.RefreshToken);
      var customClaims = GetCustomClaimsForIdToken(tokenRequestInfo.ClientId, tokenRequestInfo.RefreshToken, userDetails);
      var idToken = _jwtTokenHandler.CreateToken(tokenRequestInfo.ClientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
      var accessToken = GetAccessToken(tokenRequestInfo.ClientId, tokenRequestInfo.RefreshToken, userDetails, sid);
      return new TokenResponseInfo
      {
        IdToken = idToken,
        RefreshToken = tokenRequestInfo.RefreshToken,
        AccessToken = accessToken
      };
    }

    public string GetSAMLAuthenticationEndPoint(string clientId)
    {
      throw new NotImplementedException();
    }

    public async Task<TokenResponseInfo> GetTokensAsync(TokenRequestInfo tokenRequestInfo, string sid= null)
    {
      return await GetTokensAsync(tokenRequestInfo.ClientId, tokenRequestInfo.Code, sid);
    }

    public async Task InitiateResetPasswordAsync(ChangePasswordInitiateRequest changePasswordInitiateRequest)
    {
      await Task.CompletedTask;
    }

    public async Task<List<IdentityProviderInfoDto>> ListIdentityProvidersAsync()
    {
      await Task.CompletedTask;
      throw new NotImplementedException();
    }

    public async Task ResetMfaAsync(string userName)
    {
      await Task.CompletedTask;
    }

    public async Task<AuthResultDto> RespondToNewPasswordRequiredAsync(PasswordChallengeDto passwordChallengeDto)
    {
      await Task.CompletedTask;
      throw new NotImplementedException();
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
      await Task.CompletedTask;
    }

    public async Task SendUserActivationEmailAsync(string email, string managementApiToken = null, bool isExpired = false)
    {
      await Task.CompletedTask;
    }

    public async Task<string> SignOutAsync(string clientId, string userName)
    {
      await Task.CompletedTask;
      return _appConfigInfo.MockProvider.LoginUrl;
    }

    public async Task UpdatePendingMFAVerifiedFlagAsync(string userName, bool mfaResetVerified)
    {
      await Task.CompletedTask;
    }

    public async Task UpdateUserAsync(UserInfo userInfo)
    {
      await Task.CompletedTask;
    }

    public async Task UpdateUserMfaFlagAsync(UserInfo userInfo)
    {
      await Task.CompletedTask;
    }

    public async Task<ServiceAccessibilityResultDto> CheckServiceAccessForUserAsync(string clientId, string email)
    {
      await Task.CompletedTask;
      return new ServiceAccessibilityResultDto { IsAccessible = true };
    }


    private async Task<TokenResponseInfo> GetTokensAsync(string clientId, string email, string sid = null)
    {
      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException("TOKEN_GENERATION_FAILED");
      }

      var userDetails = await GetUserAsync(email);

      var customClaims = GetCustomClaimsForIdToken(clientId, email, userDetails);
      var idToken = _jwtTokenHandler.CreateToken(clientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);

      var accessToken = GetAccessToken(clientId, email, userDetails, sid);

      if (!userDetails.AccountVerified)
      {
        await VerifyUserAccountAsync(email);
      }

      return new TokenResponseInfo
      {
        IdToken = idToken,
        RefreshToken = email,
        AccessToken = accessToken,
        ExpiresInSeconds = _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes * 60
      };
    }

    private async Task VerifyUserAccountAsync(string email)
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_appConfigInfo.UserExternalApiDetails.UserServiceUrl);
      httpClient.DefaultRequestHeaders.Add("X-API-Key", _appConfigInfo.UserExternalApiDetails.ApiKey);
      await httpClient.PutAsync($"account-verification?user-id=" + HttpUtility.UrlEncode(email), null);
    }

    private List<ClaimInfo> GetCustomClaimsForIdToken(string clientId, string email, UserProfileInfo userProfileInfo)
    {
      var customClaims = new List<ClaimInfo>();
      // Standard claims https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
      customClaims.Add(new ClaimInfo("email", email));
      customClaims.Add(new ClaimInfo("sub", email));
      customClaims.Add(new ClaimInfo("azp", clientId));
      customClaims.Add(new ClaimInfo("name", string.Concat(userProfileInfo.FirstName, " ", userProfileInfo.LastName)));
      customClaims.Add(new ClaimInfo("given_name", userProfileInfo.FirstName));
      customClaims.Add(new ClaimInfo("family_name", userProfileInfo.LastName));
      customClaims.Add(new ClaimInfo("email_verified", "true", ClaimValueTypes.Boolean));
      return customClaims;
    }

    private string GetAccessToken(string clientId, string email, UserProfileInfo userDetails, string sid)
    {
      var rolesFromUserRoles = userDetails.Detail.RolePermissionInfo.Where(rp => rp.ServiceClientId == clientId).ToList();
      var rolesFromUserGroups = userDetails.Detail.UserGroups.Where(ug => ug.ServiceClientId == clientId).ToList();
      if (rolesFromUserRoles.Any() || rolesFromUserGroups.Any())
      {
        var roles = rolesFromUserRoles.Select(r => r.RoleKey).Concat(rolesFromUserGroups.Select(r => r.AccessRole)).Distinct();
        var accesstokenClaims = new List<ClaimInfo>();
        accesstokenClaims.Add(new ClaimInfo("uid", userDetails.Detail.Id.ToString()));
        accesstokenClaims.Add(new ClaimInfo("caller", "user"));
        accesstokenClaims.Add(new ClaimInfo("ciiOrgId", userDetails.OrganisationId));
        if (!string.IsNullOrWhiteSpace(sid))
        {
          accesstokenClaims.Add(new ClaimInfo("sid", sid));
        }
        foreach (var role in roles)
        {
          accesstokenClaims.Add(new ClaimInfo("roles", role));
        }

        accesstokenClaims.Add(new ClaimInfo("sub", email));
        var accessToken = _jwtTokenHandler.CreateToken(clientId, accesstokenClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
        return accessToken;
      }
      throw new UnauthorizedAccessException();
    }

    private async Task<UserProfileInfo> GetUserAsync(string email)
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_appConfigInfo.UserExternalApiDetails.UserServiceUrl);
      httpClient.DefaultRequestHeaders.Add("X-API-Key", _appConfigInfo.UserExternalApiDetails.ApiKey);
      var result = await httpClient.GetAsync($"?user-id={HttpUtility.UrlEncode(email)}");
      var userJsonString = await result.Content.ReadAsStringAsync();
      if (!string.IsNullOrEmpty(userJsonString))
      {
        var userDetails = JsonConvert.DeserializeObject<UserProfileInfo>(userJsonString);
        return userDetails;
      }
      throw new RecordNotFoundException();
    }

    public async Task<string> GetActivationEmailVerificationLink(string email)
    {
      await Task.CompletedTask;
      return null;
    }
    public async Task<string> GetSidFromRefreshToken(string refreshToken, string sid)
    {
      await Task.CompletedTask;
      return null;
    }
  }
}
