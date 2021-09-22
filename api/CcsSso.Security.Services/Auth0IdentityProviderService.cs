using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Security.Services
{
  public class Auth0IdentityProviderService : IIdentityProviderService
  {
    IAuthenticationApiClient _authenticationApiClient;
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly TokenHelper _tokenHelper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJwtTokenHandler _jwtTokenHandler;

    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    public Auth0IdentityProviderService(ApplicationConfigurationInfo appConfigInfo, TokenHelper tokenHelper,
      IHttpClientFactory httpClientFactory, ICcsSsoEmailService ccsSsoEmailService, IJwtTokenHandler jwtTokenHandler)
    {
      _appConfigInfo = appConfigInfo;
      _authenticationApiClient = new AuthenticationApiClient(_appConfigInfo.Auth0ConfigurationInfo.Domain);
      _tokenHelper = tokenHelper;
      _httpClientFactory = httpClientFactory;
      _ccsSsoEmailService = ccsSsoEmailService;
      _jwtTokenHandler = jwtTokenHandler;
    }

    /// <summary>
    /// Authenticates and issues tokens. Following Auth0 configurations are required
    /// Enable "Allow Offline Access" (https://auth0.com/docs/flows/call-your-api-using-resource-owner-password-flow)
    /// Enable Password grant type (Applications->Settings-> Advanced Settings->Grant Types (Password, RefreshToken)
    /// Set default connection name (Auth0 database connection name) (Profile->Settings->API Authorization Settings->Default Directory)
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="userPassword"></param>
    /// <returns></returns>
    public async Task<AuthResultDto> AuthenticateAsync(string clientId, string secret, string userName, string userPassword)
    {
      try
      {
        ResourceOwnerTokenRequest resourceOwnerTokenRequest = new ResourceOwnerTokenRequest()
        {
          Username = userName,
          Password = userPassword,
          ClientId = clientId,
          Scope = "offline_access" //Need this to receive a refresh token
        };

        if (!string.IsNullOrEmpty(secret))
        {
          resourceOwnerTokenRequest.ClientSecret = secret;
        }

        var result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        if (result != null)
        {
          var tokenInfo = await GetTokensAsync(clientId, result);
          return new AuthResultDto()
          {
            AccessToken = tokenInfo.AccessToken,
            IdToken = tokenInfo.IdToken,
            RefreshToken = tokenInfo.RefreshToken
          };
        }
        throw new UnauthorizedAccessException();
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.Error == "invalid_grant") // This is the same error which we get for password reset required and invalid username/password
        {
          throw new CcsSsoException("INVALID_USERNAME_PASSWORD");
        }
        else if (e.ApiError.Error == "access_denied")
        {
          throw new CcsSsoException("INVALID_CLIENT_CONFIGURATION");
        }
        else if (e.ApiError.Error == "invalid_request")
        {
          throw new CcsSsoException("MISSING_REQUIRED_PARAMETERS");
        }
        throw new UnauthorizedAccessException();
      }
    }

    public async Task<UserRegisterResult> CreateUserAsync(Domain.Dtos.UserInfo userInfo)
    {
      try
      {
        UserCreateRequest userCreateRequest = new UserCreateRequest
        {
          Email = userInfo.Email,
          Password = UtilitiesHelper.GenerateRandomPassword(_appConfigInfo.PasswordPolicy),
          FirstName = userInfo.FirstName,
          LastName = userInfo.LastName,
          EmailVerified = false,
          UserMetadata = new
          {
            use_mfa = userInfo.MfaEnabled
          },
          Connection = _appConfigInfo.Auth0ConfigurationInfo.DBConnectionName
        };

        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var result = await _managementApiClient.Users.CreateAsync(userCreateRequest);

          await SendUserActivationEmailAsync(userInfo.Email, managementApiToken);

          return new UserRegisterResult()
          {
            UserName = result.Email,
            Id = result.UserId
          };
        }
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.Error == "Conflict")
        {
          throw new CcsSsoException("USERNAME_EXISTS");
        }
        else
        {
          throw new CcsSsoException("USER_REGISTRATION_FAILED");
        }
      }
    }

    public async Task SendUserActivationEmailAsync(string email, string managementApiToken = null)
    {
      if (string.IsNullOrEmpty(managementApiToken))
      {
        managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      }
      
      var ticket = await GetResetPasswordTicketAsync(email, managementApiToken, _appConfigInfo.CcsEmailConfigurationInfo.UserActivationLinkTTLInMinutes);

      if (!string.IsNullOrEmpty(ticket))
      {
        ticket = ticket + "&initial";
        await _ccsSsoEmailService.SendUserActivationLinkAsync(email, ticket);
      }
    }


    public async Task UpdateUserAsync(Domain.Dtos.UserInfo userInfo)
    {
      try
      {
        UserUpdateRequest userUpdateRequest = new UserUpdateRequest
        {
          Email = userInfo.Email,
          FullName = userInfo.FirstName,
          LastName = userInfo.LastName,
          Connection = _appConfigInfo.Auth0ConfigurationInfo.DBConnectionName
        };

        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var result = await _managementApiClient.Users.UpdateAsync(userInfo.Id, userUpdateRequest);
        }
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.ErrorCode == "invalid_uri")
        {
          throw new RecordNotFoundException();
        }
        else
        {
          throw new CcsSsoException("USER_UPDATE_FAILED");
        }
      }
    }

    public async Task ResetMfaAsync(string userName)
    {
      try
      {
        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(userName)).FirstOrDefault();
          if (user != null)
          {
            var enrollments = await _managementApiClient.Users.GetEnrollmentsAsync(user.UserId);
            foreach (var enrollment in enrollments)
            {
              await _managementApiClient.Guardian.DeleteEnrollmentAsync(enrollment.Id);
            }
          }
          else
          {
            throw new RecordNotFoundException();
          }
        }
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.ErrorCode == "invalid_uri")
        {
          throw new RecordNotFoundException();
        }
        else
        {
          throw new CcsSsoException("MFA_RESET_FAILED");
        }
      }
    }

    public async Task UpdateUserMfaFlagAsync(Domain.Dtos.UserInfo userInfo)
    {
      try
      {
        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(userInfo.Email)).FirstOrDefault();
          if (user != null)
          {
            UserUpdateRequest userUpdateRequest = new UserUpdateRequest
            {
              UserMetadata = new
              {
                use_mfa = userInfo.MfaEnabled
              }
            };

            await _managementApiClient.Users.UpdateAsync(user.UserId, userUpdateRequest);
          }
        }
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.ErrorCode == "invalid_uri")
        {
          throw new RecordNotFoundException();
        }
        else
        {
          throw new CcsSsoException("USER_UPDATE_FAILED");
        }
      }
    }

    public async Task UpdatePendingMFAVerifiedFlagAsync(string userName, bool mfaResetVerified)
    {
      try
      {
        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(userName)).FirstOrDefault();
          if (user != null)
          {
            UserUpdateRequest userUpdateRequest = new UserUpdateRequest
            {
              UserMetadata = new
              {
                mfa_reset_verified = mfaResetVerified
              }
            };

            await _managementApiClient.Users.UpdateAsync(user.UserId, userUpdateRequest);
          }
        }
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.ErrorCode == "invalid_uri")
        {
          throw new RecordNotFoundException();
        }
        else
        {
          throw new CcsSsoException("USER_UPDATE_FAILED");
        }
      }
    }

    public async Task<TokenResponseInfo> GetRenewedTokensAsync(string clientId, string clientSecret, string refreshToken, string sid)
    {
      try
      {
        RefreshTokenRequest resourceOwnerTokenRequest = new RefreshTokenRequest()
        {
          ClientId = clientId,
          RefreshToken = refreshToken,
          Scope = "email offline_access openid profile" //Need this to receive a refresh token
        };

        if (!string.IsNullOrEmpty(clientSecret))
        {
          resourceOwnerTokenRequest.ClientSecret = clientSecret;
        }

        var result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        if (result != null)
        {
          var tokenDecoded = _jwtTokenHandler.DecodeToken(result.IdToken);
          var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
          if (string.IsNullOrEmpty(email))
          {
            throw new CcsSsoException("TOKEN_GENERATION_FAILED");
          }

          var userDetails = await GetUserAsync(email);
          var customClaims = GetCustomClaimsForIdToken(tokenDecoded, clientId, email, sid, userDetails);
          var idToken = _jwtTokenHandler.CreateToken(clientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
          var accessToken = GetAccessToken(clientId, email, userDetails);
          return new TokenResponseInfo
          {
            IdToken = idToken,
            RefreshToken = result.RefreshToken,
            AccessToken = accessToken
          };
        }
        throw new UnauthorizedAccessException();
      }
      catch (ErrorApiException e)
      {
        throw new SecurityException(new ErrorInfo()
        {
          Error = e.ApiError.Error,
          ErrorDescription = e.ApiError.Message
        });
      }
    }

    public async Task<TokenResponseInfo> GetTokensAsync(TokenRequestInfo tokenRequestInfo, string sid = null)
    {
      try
      {
        AccessTokenResponse result;

        if (string.IsNullOrEmpty(tokenRequestInfo.CodeVerifier))
        {
          var resourceOwnerTokenRequest = new AuthorizationCodeTokenRequest()
          {
            ClientId = tokenRequestInfo.ClientId,
            RedirectUri = tokenRequestInfo.RedirectUrl,
            Code = tokenRequestInfo.Code
          };

          if (!string.IsNullOrEmpty(tokenRequestInfo.ClientSecret))
          {
            resourceOwnerTokenRequest.ClientSecret = tokenRequestInfo.ClientSecret;
          }

          result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        }
        else
        {
          var resourceOwnerTokenRequest = new AuthorizationCodePkceTokenRequest()
          {
            ClientId = tokenRequestInfo.ClientId,
            RedirectUri = tokenRequestInfo.RedirectUrl,
            Code = tokenRequestInfo.Code,
            CodeVerifier = tokenRequestInfo.CodeVerifier,
          };
          result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        }
        var tokenInfo = await GetTokensAsync(tokenRequestInfo.ClientId, result, sid);
        return tokenInfo;
      }
      catch (ErrorApiException e)
      {
        throw new SecurityException(new ErrorInfo()
        {
          Error = e.ApiError.Error,
          ErrorDescription = e.ApiError.Message
        });
      }
    }

    public async Task<List<IdentityProviderInfoDto>> ListIdentityProvidersAsync()
    {
      List<IdentityProviderInfoDto> identityProviderInfoDtos = new List<IdentityProviderInfoDto>();

      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        try
        {
          GetConnectionsRequest getConnectionsRequest = new GetConnectionsRequest();
          PaginationInfo paginationInfo = new PaginationInfo();

          var connections = await _managementApiClient.Connections.GetAllAsync(getConnectionsRequest, paginationInfo);

          foreach (var connection in connections)
          {
            identityProviderInfoDtos.Add(new IdentityProviderInfoDto
            {
              Name = connection.Name,
            });
          }
        }
        catch (ErrorApiException)
        {
          throw new UnauthorizedAccessException();
        }
      }
      return identityProviderInfoDtos;
    }

    /// <summary>
    /// Change the password of the user by updating the user.
    /// Both UserId and NewPassword Required to chnage password.
    /// </summary>
    /// <param name="changePasswordDto"></param>
    /// <returns></returns>
    public async Task ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {

      await ValidateCurrentPasswordAsync(changePasswordDto);

      UserUpdateRequest userUpdateRequest = new UserUpdateRequest
      {
        ClientId = _appConfigInfo.Auth0ConfigurationInfo.ClientId,
        Connection = _appConfigInfo.Auth0ConfigurationInfo.DBConnectionName,
        Password = changePasswordDto.NewPassword,
      };

      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        try
        {
          var users = await _managementApiClient.Users.GetUsersByEmailAsync(changePasswordDto.UserName);
          var userId = users.Select(u => u.UserId).FirstOrDefault();
          await _managementApiClient.Users.UpdateAsync(userId, userUpdateRequest);
        }
        catch (ErrorApiException ex)
        {
          if (ex.ApiError.Error == "Bad Request")
          {
            if (ex.ApiError.Message == "PasswordNoUserInfoError: Password contains user information")
            {
              throw new CcsSsoException("ERROR_PASSWORD_CONTAINS_USER_INFO");
            }
            else if (ex.ApiError.Message == "PasswordHistoryError: Password has previously been used")
            {
              throw new CcsSsoException("ERROR_PASSWORD_ALREADY_USED");
            }
            else if (ex.ApiError.Message == "PasswordStrengthError: Password is too weak")
            {
              throw new CcsSsoException("ERROR_PASSWORD_TOO_WEAK");
            }
            else
            {
              throw new CcsSsoException("INVALID_PASSWORD");
            }
          }
          throw new UnauthorizedAccessException();
        }
      }
    }

    Task<AuthResultDto> IIdentityProviderService.RespondToNewPasswordRequiredAsync(PasswordChallengeDto passwordChallengeDto)
    {
      throw new NotImplementedException();
    }

    public async Task InitiateResetPasswordAsync(ChangePasswordInitiateRequest changePasswordInitiateRequest)
    {
      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        var ticket = await GetResetPasswordTicketAsync(changePasswordInitiateRequest.UserName, managementApiToken, _appConfigInfo.CcsEmailConfigurationInfo.ResetPasswordLinkTTLInMinutes);
        if (!string.IsNullOrEmpty(ticket))
        {
          var users = await _managementApiClient.Users.GetUsersByEmailAsync(changePasswordInitiateRequest.UserName);
          var userId = users.Select(u => u.UserId).FirstOrDefault();

          UserUpdateRequest userUpdateRequest = new UserUpdateRequest
          {
            EmailVerified = !changePasswordInitiateRequest.ForceLogout
          };
          await _managementApiClient.Users.UpdateAsync(userId, userUpdateRequest);
          await _ccsSsoEmailService.SendResetPasswordAsync(changePasswordInitiateRequest.UserName, ticket);
        }
      }
    }

    Task IIdentityProviderService.ResetPasswordAsync(ResetPasswordDto resetPassword)
    {
      throw new NotImplementedException();
    }

    public async Task<string> SignOutAsync(string clientId, string returnTo)
    {
      // Should include "federated" as query string para if requires to signout from federated auth providers such as Google,FB
      var url = $"{_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl}/v2/logout?client_id={clientId}" +
                   $"&returnTo={returnTo}";

      return url;
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl);
      var url = "oauth/revoke";

      var list = new List<KeyValuePair<string, string>>();
      list.Add(new KeyValuePair<string, string>("token", refreshToken));
      list.Add(new KeyValuePair<string, string>("client_id", _appConfigInfo.Auth0ConfigurationInfo.ClientId));
      list.Add(new KeyValuePair<string, string>("client_secret", _appConfigInfo.Auth0ConfigurationInfo.ClientSecret));

      HttpContent codeContent = new FormUrlEncodedContent(list);
      await client.PostAsync(url, codeContent);
    }

    public string GetAuthenticationEndPoint(string state, string scope, string response_type, string client_id, string redirect_uri,
      string code_challenge_method, string code_challenge, string prompt, string nonce, string display, string login_hint, int? max_age, string acr_values)
    {
      string uri = $"{_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl}/authorize?client_id={client_id}" +
                  $"&response_type={response_type}&scope={scope}&redirect_uri={redirect_uri}&state={state}";

      if (!string.IsNullOrEmpty(code_challenge_method))
      {
        uri = uri + "&code_challenge_method=" + code_challenge_method;
      }

      if (!string.IsNullOrEmpty(code_challenge))
      {
        uri = uri + "&code_challenge=" + code_challenge;
      }

      if (!string.IsNullOrEmpty(prompt))
      {
        uri = uri + $"&prompt={prompt}";
      }

      if (!string.IsNullOrEmpty(nonce))
      {
        uri = uri + $"&nonce={nonce}";
      }

      if (!string.IsNullOrEmpty(display))
      {
        uri = uri + $"&display={display}";
      }

      if (!string.IsNullOrEmpty(login_hint))
      {
        uri = uri + $"&login_hint={login_hint}";
      }

      if (max_age.HasValue)
      {
        uri = uri + $"&max_age={max_age}";
      }

      if (!string.IsNullOrEmpty(acr_values))
      {
        uri = uri + $"&acr_values={acr_values}";
      }

      return uri;
    }

    public async Task DeleteAsync(string email)
    {
      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        try
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(email)).FirstOrDefault();
          if (user != null)
          {
            await _managementApiClient.Users.DeleteAsync(user.UserId);
          }
          else
          {
            throw new RecordNotFoundException();
          }
        }
        catch (ErrorApiException e)
        {
          if (e.ApiError.ErrorCode == "invalid_query_string")
          {
            throw new CcsSsoException("INVALID_EMAIL");
          }
        }
      }
    }

    public async Task<IdamUser> GetUser(string email)
    {
      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        try
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(email)).FirstOrDefault();
          if (user != null)
          {
            return new IdamUser()
            {
              FirstName = user.FirstName,
              LastName = user.LastName,
              EmailVerified = user.EmailVerified.HasValue ? user.EmailVerified.Value : false
            };
          }
          else
          {
            throw new RecordNotFoundException();
          }
        }
        catch (ErrorApiException e)
        {
          if (e.ApiError.ErrorCode == "invalid_query_string")
          {
            throw new CcsSsoException("INVALID_EMAIL");
          }
          return null;
        }
      }
    }

    public async Task<string> GetIdentityProviderAuthenticationEndPointAsync()
    {
      List<string> scopes = new List<string>() { "openid", "offline_access" };

      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        try
        {
          var client = await _managementApiClient.Clients.GetAsync(_appConfigInfo.Auth0ConfigurationInfo.ClientId);
          if (client != null)
          {
            var url = $"{_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl}/authorize?client_id={_appConfigInfo.Auth0ConfigurationInfo.ClientId}&response_type=code" +
                      $"&scope={string.Join("+", scopes)}&redirect_uri={client.Callbacks.First()}";
            return url;
          }
        }
        catch (ErrorApiException)
        {
          throw new UnauthorizedAccessException();
        }
      }
      return null;
    }

    private async Task<string> GetResetPasswordTicketAsync(string userName, string managementApiToken, int expirationTimeInMinutes)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managementApiToken);
      client.BaseAddress = new Uri(_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl);

      var url = "/api/v2/tickets/password-change";

      var userActivationLinkTTLInSeconds = expirationTimeInMinutes * 60;

      var list = new List<KeyValuePair<string, string>>();
      list.Add(new KeyValuePair<string, string>("email", userName));
      list.Add(new KeyValuePair<string, string>("ttl_sec", userActivationLinkTTLInSeconds.ToString()));
      list.Add(new KeyValuePair<string, string>("mark_email_as_verified", "true"));
      list.Add(new KeyValuePair<string, string>("connection_id", _appConfigInfo.Auth0ConfigurationInfo.DefaultDBConnectionId));
      list.Add(new KeyValuePair<string, string>("client_id", _appConfigInfo.Auth0ConfigurationInfo.ClientId));

      HttpContent codeContent = new FormUrlEncodedContent(list);
      var response = await client.PostAsync(url, codeContent);
      if (response.StatusCode == System.Net.HttpStatusCode.Created)
      {
        var ticket = await response.Content.ReadAsStringAsync();
        var ticketInfo = JObject.Parse(ticket).ToObject<Dictionary<string, string>>();
        return ticketInfo.FirstOrDefault().Value;
      }
      return null;
    }

    private async Task ValidateCurrentPasswordAsync(ChangePasswordDto changePasswordDto)
    {
      try
      {
        ResourceOwnerTokenRequest resourceOwnerTokenRequest = new ResourceOwnerTokenRequest()
        {
          Username = changePasswordDto.UserName,
          Password = changePasswordDto.OldPassword,
          ClientId = _appConfigInfo.Auth0ConfigurationInfo.ClientId,
          ClientSecret = _appConfigInfo.Auth0ConfigurationInfo.ClientSecret
        };
        var result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.Error == "invalid_grant")
        {
          throw new CcsSsoException("INVALID_CURRENT_PASSWORD");
        }
      }
    }

    private async Task<UserProfileInfo> GetUserAsync(string email)
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_appConfigInfo.UserExternalApiDetails.Url);
      httpClient.DefaultRequestHeaders.Add("X-API-Key", _appConfigInfo.UserExternalApiDetails.ApiKey);
      var result = await httpClient.GetAsync($"?userId={HttpUtility.UrlEncode(email)}");
      var userJsonString = await result.Content.ReadAsStringAsync();
      if (!string.IsNullOrEmpty(userJsonString))
      {
        var userDetails = JsonConvert.DeserializeObject<UserProfileInfo>(userJsonString);
        return userDetails;
      }
      throw new RecordNotFoundException();
    }

    private string GetAccessToken(string clientId, string email, UserProfileInfo userDetails)
    {
      var rolesFromUserRoles = userDetails.Detail.RolePermissionInfo.Where(rp => rp.ServiceClientId == clientId).ToList();
      var rolesFromUserGroups = userDetails.Detail.UserGroups.Where(ug => ug.ServiceClientId == clientId).ToList();
      if (rolesFromUserRoles.Any() || rolesFromUserGroups.Any())
      {
        var roles = rolesFromUserRoles.Select(r => r.RoleKey).Concat(rolesFromUserGroups.Select(r => r.AccessRole)).Distinct();
        var accesstokenClaims = new List<ClaimInfo>();
        accesstokenClaims.Add(new ClaimInfo("uid", userDetails.Detail.Id.ToString()));
        accesstokenClaims.Add(new ClaimInfo("ciiOrgId", userDetails.OrganisationId));
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

    public async Task SendNominateEmailAsync(Domain.Dtos.UserInfo userInfo)
    {
      try
      {
        UserCreateRequest userCreateRequest = new UserCreateRequest
        {
          Email = userInfo.Email,
          FirstName = userInfo.FirstName,
          LastName = userInfo.LastName,
        };

        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var link = String.Empty;
          if (_appConfigInfo.Auth0ConfigurationInfo.Domain.StartsWith("sand"))
          {
            link = "https://sand-ccs-sso.london.cloudapps.digital/manage-org/register";
          }
          else if (_appConfigInfo.Auth0ConfigurationInfo.Domain.StartsWith("test"))
          {
            link = "https://test-ccs-sso.london.cloudapps.digital/manage-org/register";
          }
          else
          {
            link = "https://dev-ccs-sso.london.cloudapps.digital/manage-org/register";
          }
          await _ccsSsoEmailService.SendNominateEmailAsync(userInfo.Email, link);
        }
      }
      catch (Exception e)
      {
        Console.Write(e);
        throw;
      }
    }

    private async Task<TokenResponseInfo> GetTokensAsync(string clientId, AccessTokenResponse accessTokenResponse, string sid = null)
    {
      var tokenDecoded = _jwtTokenHandler.DecodeToken(accessTokenResponse.IdToken);
      var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException("TOKEN_GENERATION_FAILED");
      }

      var useMfa = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "https://ccs-sso/use_mfa")?.Value;
      if(useMfa != null)
      {
        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(email)).FirstOrDefault();
          if (user != null && user.UserMetadata != null && user.UserMetadata.mfa_reset_verified == false)
          {
            throw new CcsSsoException("MFA_NOT_VERIFIED");
          }
        }
      }

      var connection = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "https://ccs-sso/connection")?.Value;
      var userDetails = await GetUserAsync(email);
      if (userDetails.Detail.IdentityProvider != connection)
      {
        throw new CcsSsoException("INVALID_CONNECTION");
      }

      var customClaims = GetCustomClaimsForIdToken(tokenDecoded, clientId, email, sid, userDetails);
      var idToken = _jwtTokenHandler.CreateToken(clientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);

      var accessToken = GetAccessToken(clientId, email, userDetails);

      return new TokenResponseInfo
      {
        IdToken = idToken,
        TokenType = accessTokenResponse.TokenType,
        RefreshToken = accessTokenResponse.RefreshToken,
        AccessToken = accessToken,
        ExpiresInSeconds = _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes * 60
      };
    }

    private List<ClaimInfo> GetCustomClaimsForIdToken(JwtSecurityToken tokenDecoded, string clientId, string email, string sid, UserProfileInfo userProfileInfo)
    {
      var customClaims = new List<ClaimInfo>();
      // Standard claims https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
      customClaims.Add(new ClaimInfo("email", email));

      if (!string.IsNullOrEmpty(sid))
      {
        customClaims.Add(new ClaimInfo("sid", sid));
      }

      customClaims.Add(new ClaimInfo("sub", email));
      customClaims.Add(new ClaimInfo("azp", clientId));
      customClaims.Add(new ClaimInfo("name", string.Concat(userProfileInfo.FirstName, " ", userProfileInfo.LastName)));
      customClaims.Add(new ClaimInfo("given_name", userProfileInfo.FirstName));
      customClaims.Add(new ClaimInfo("family_name", userProfileInfo.LastName));
      customClaims.Add(new ClaimInfo("amr", "pwd")); // At the moment it's only password based https://tools.ietf.org/html/rfc8176. Revisit for Phase1


      var authTime = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "auth_time")?.Value;
      if (!string.IsNullOrEmpty(authTime))
      {
        customClaims.Add(new ClaimInfo("auth_time", authTime, ClaimValueTypes.Integer64));
      }

      var nonce = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;
      if (!string.IsNullOrEmpty(nonce))
      {
        customClaims.Add(new ClaimInfo("nonce", nonce));
      }

      var email_verified = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
      if (!string.IsNullOrEmpty(email_verified))
      {
        customClaims.Add(new ClaimInfo("email_verified", email_verified, ClaimValueTypes.Boolean));
      }
      return customClaims;
    }
  }
}
