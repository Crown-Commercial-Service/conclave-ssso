using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using CcsSso.Domain.Constants;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
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
using static CcsSso.Security.Domain.Constants.Constants;

namespace CcsSso.Security.Services
{
  public class Auth0IdentityProviderService : IIdentityProviderService
  {
    IAuthenticationApiClient _authenticationApiClient;
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly TokenHelper _tokenHelper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJwtTokenHandler _jwtTokenHandler;
    private readonly ISecurityCacheService _securityCacheService;

    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly IRemoteCacheService _remoteCacheService;

    private readonly IAwsDataSqsService _awsDataSqsService;

    public Auth0IdentityProviderService(ApplicationConfigurationInfo appConfigInfo, TokenHelper tokenHelper,
      IHttpClientFactory httpClientFactory, ICcsSsoEmailService ccsSsoEmailService, IJwtTokenHandler jwtTokenHandler,
      ISecurityCacheService securityCacheService, IRemoteCacheService remoteCacheService, IAwsDataSqsService awsDataSqsService)
    {
      _appConfigInfo = appConfigInfo;
      _authenticationApiClient = new AuthenticationApiClient(_appConfigInfo.Auth0ConfigurationInfo.Domain);
      _tokenHelper = tokenHelper;
      _httpClientFactory = httpClientFactory;
      _ccsSsoEmailService = ccsSsoEmailService;
      _jwtTokenHandler = jwtTokenHandler;
      _securityCacheService = securityCacheService;
      _remoteCacheService = remoteCacheService;
      _awsDataSqsService = awsDataSqsService;
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
          var sid = Guid.NewGuid().ToString();
          var tokenInfo = await GetTokensAsync(clientId, result, sid);
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
          Password = !string.IsNullOrEmpty(userInfo.Password) ? userInfo.Password : UtilitiesHelper.GenerateRandomPassword(_appConfigInfo.PasswordPolicy),
          FirstName = userInfo.FirstName,
          LastName = userInfo.LastName,
          EmailVerified = !string.IsNullOrEmpty(userInfo.Password),
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

          if (userInfo.SendUserRegistrationEmail)
          {
            await SendUserActivationEmailAsync(userInfo.Email, managementApiToken);
          }

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
        else if (e.ApiError.Error == "Bad Request")
        {
          switch (e.ApiError.Message)
          {
            case "PasswordStrengthError: Password is too weak":
              throw new CcsSsoException("ERROR_PASSWORD_TOO_WEAK");
            default:
              throw new CcsSsoException("USER_REGISTRATION_FAILED");
          }
        }
        else
        {
          throw new CcsSsoException("USER_REGISTRATION_FAILED");
        }
      }
      catch (RateLimitApiException)
      {
        await PushCreateUserMessageToDataQueueAsync(userInfo);

        return new UserRegisterResult()
        {
          UserName = userInfo.Email,
          Id = userInfo.Id
        };
      }
    }

    public async Task SendUserActivationEmailAsync(string email, string managementApiToken = null, bool isExpired = false)
    {
      var isActivationEmail = true;

      if (isExpired) // If link expired check whether link was useractivation or resetpassword
      {
        var user = await GetIdamUserByEmailAsync(email);
        isActivationEmail = user.LoginCount == 0;
      }

      if (string.IsNullOrEmpty(managementApiToken))
      {
        managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      }

      var ticket = await GetResetPasswordTicketAsync(email, managementApiToken,
        isActivationEmail ? _appConfigInfo.CcsEmailConfigurationInfo.UserActivationLinkTTLInMinutes : _appConfigInfo.CcsEmailConfigurationInfo.ResetPasswordLinkTTLInMinutes);

      if (!string.IsNullOrEmpty(ticket))
      {
        ticket += "&initial";
        if (isActivationEmail)
        {
          await _ccsSsoEmailService.SendUserActivationLinkAsync(email, ticket);
        }
        else
        {
          await _ccsSsoEmailService.SendResetPasswordAsync(email, ticket);
        }
      }
    }

    public async Task<string> GetActivationEmailVerificationLink(string email)
    {
      var user = await GetIdamUserByEmailAsync(email);

      bool isActivationEmail = user.LoginCount == 0;

      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();

      var ticket = await GetResetPasswordTicketAsync(email, managementApiToken,
        isActivationEmail ? _appConfigInfo.CcsEmailConfigurationInfo.UserActivationLinkTTLInMinutes : _appConfigInfo.CcsEmailConfigurationInfo.ResetPasswordLinkTTLInMinutes);

      if (!string.IsNullOrEmpty(ticket))
      {
        ticket += "&initial";
        return ticket;

      }
      return null;
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

        if (!string.IsNullOrEmpty(userInfo.Password))
        {
          userUpdateRequest.Password = userInfo.Password;
        }

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
          var users = (await _managementApiClient.Users.GetUsersByEmailAsync(userName)).ToList();
          if (users != null && users.Count > 0)
          {
            foreach (var user in users)
            {
              var enrollments = await _managementApiClient.Users.GetEnrollmentsAsync(user.UserId);
              foreach (var enrollment in enrollments)
              {
                await _managementApiClient.Guardian.DeleteEnrollmentAsync(enrollment.Id);
              }
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
          var users = (await _managementApiClient.Users.GetUsersByEmailAsync(userInfo.Email)).ToList();
          if (users != null && users.Count > 0)
          {
            if (!userInfo.MfaEnabled) // reset MFA when the MFA is turned Off
            {
              try
              {
                await ResetMfaAsync(users[0].Email);
              }
              catch (Exception ex)
              {
                Console.WriteLine($"Exception while resetting mfa before disable MFA from Auth0. Error Message - {ex.Message}");
              }
            }

            var allTask = new List<Task>();

            foreach (var user in users)
            {
              UserUpdateRequest userUpdateRequest = new UserUpdateRequest
              {
                UserMetadata = new
                {
                  use_mfa = userInfo.MfaEnabled
                }
              };

              allTask.Add(_managementApiClient.Users.UpdateAsync(user.UserId, userUpdateRequest));

            }
            await Task.WhenAll(allTask);
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
          var users = (await _managementApiClient.Users.GetUsersByEmailAsync(userName)).ToList();
          if (users != null && users.Count > 0)
          {
            var allTask = new List<Task>();
            foreach (var user in users)
            {
              UserUpdateRequest userUpdateRequest = new UserUpdateRequest
              {
                UserMetadata = new
                {
                  mfa_reset_verified = mfaResetVerified
                }
              };

              allTask.Add(_managementApiClient.Users.UpdateAsync(user.UserId, userUpdateRequest));

            }
            await Task.WhenAll(allTask);
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

    public async Task<TokenResponseInfo> GetMachineTokenAsync(string clientId, string clientSecret, string audience)
    {
      try
      {
        ClientCredentialsTokenRequest clientCredentialsTokenRequest = new ClientCredentialsTokenRequest()
        {
          ClientId = clientId,
          Audience = _appConfigInfo.Auth0ConfigurationInfo.DefaultAudience
        };

        if (!string.IsNullOrEmpty(clientSecret))
        {
          clientCredentialsTokenRequest.ClientSecret = clientSecret;
        }

        var result = await _authenticationApiClient.GetTokenAsync(clientCredentialsTokenRequest);
        if (result != null)
        {
          var serviceProfile = await GetServiceProfileAsync(clientId, audience);
          var accessToken = GetAccessToken(clientId, audience, serviceProfile);
          return new TokenResponseInfo
          {
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

    public async Task<TokenResponseInfo> GetRenewedTokensAsync(TokenRequestInfo tokenRequestInfo, string sid)
    {
      try
      {
        RefreshTokenRequest resourceOwnerTokenRequest = new RefreshTokenRequest()
        {
          ClientId = tokenRequestInfo.ClientId,
          RefreshToken = tokenRequestInfo.RefreshToken,
          Scope = "email offline_access openid profile" //Need this to receive a refresh token
        };

        if (!string.IsNullOrEmpty(tokenRequestInfo.ClientSecret))
        {
          resourceOwnerTokenRequest.ClientSecret = tokenRequestInfo.ClientSecret;
        }

        // get the sid from refresh token sent from client.
        sid = await GetSidFromRefreshToken(tokenRequestInfo.RefreshToken, sid);

        // #Delegated
        string delegatedOrgId = await MapDelegatedOrgIdWithSid(sid, tokenRequestInfo.DelegatedOrgId);

        var result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        if (result != null)
        {
          // store the sid against the refresh token returned from Auth0.
          await AttachSidWithRefreshTokenAsync(result.RefreshToken, sid);

          var tokenDecoded = _jwtTokenHandler.DecodeToken(result.IdToken);
          var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
          if (string.IsNullOrEmpty(email))
          {
            throw new CcsSsoException("TOKEN_GENERATION_FAILED");
          }
          // #Delegated
          var userDetails = await GetUserAsync(email, delegatedOrgId);
          var customClaims = GetCustomClaimsForIdToken(tokenDecoded, tokenRequestInfo.ClientId, email, sid, userDetails);
          var idToken = _jwtTokenHandler.CreateToken(tokenRequestInfo.ClientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
          var accessToken = GetAccessToken(tokenRequestInfo.ClientId, email, userDetails, sid, delegatedOrgId);
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

    public async Task<string> GetSidFromRefreshToken(string refreshToken, string sid)
    {
      var sidCache = await _securityCacheService.GetValueAsync<string>(refreshToken);
      if (!string.IsNullOrEmpty(sidCache))
      {
        sid = sidCache;
      }

      return sid;
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
        // #Delegated
        var tokenInfo = await GetTokensAsync(tokenRequestInfo.ClientId, result, sid, tokenRequestInfo.DelegatedOrgId);
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
      string resetAttemptRedisKey = "ResetRequestAttempt-" + changePasswordInitiateRequest.UserName;
      var userResetEmailAttemptInfo = await _remoteCacheService.GetValueAsync<string>(resetAttemptRedisKey);
      int userResetEmailAttempts = Convert.ToInt32(userResetEmailAttemptInfo?.Split("|")[0]);
      DateTime userResetEmailFirstAttemptTime = Convert.ToDateTime(userResetEmailAttemptInfo?.Split("|")[1]);

      // Admin reset password then not threshold validation otherwise validate
      if (!changePasswordInitiateRequest.ForceLogout)
      {
        // Attempt threshold validation
        if (userResetEmailAttempts >= Convert.ToInt32(_appConfigInfo.ResetPasswordSettings.MaxAllowedAttempts))
        {
          throw new CcsSsoException(ErrorCodes.MaxPasswordResetAttempt);
        }
        else if (userResetEmailAttempts == 0)
        {
          await _remoteCacheService.SetValueAsync<string>(resetAttemptRedisKey, (userResetEmailAttempts + 1) + "|" + DateTime.UtcNow,
          new TimeSpan(0, Convert.ToInt32(_appConfigInfo.ResetPasswordSettings.MaxAllowedAttemptsThresholdInMinutes), 0));
        }
        else
        {
          var elepsMinutsForExpiration = DateTime.UtcNow.Subtract(userResetEmailFirstAttemptTime).Minutes;
          elepsMinutsForExpiration = elepsMinutsForExpiration > 1 ? elepsMinutsForExpiration : 1;
          var remianingMinutsForExpiration = Convert.ToInt32(_appConfigInfo.ResetPasswordSettings.MaxAllowedAttemptsThresholdInMinutes) - elepsMinutsForExpiration;
          await _remoteCacheService.SetValueAsync<string>(resetAttemptRedisKey, (userResetEmailAttempts + 1) + "|" + userResetEmailFirstAttemptTime, new TimeSpan(0, remianingMinutsForExpiration, 0));
        }
      }

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

    public string GetSAMLAuthenticationEndPoint(string clientId)
    {
      string uri = $"{_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl}/samlp/{clientId}";
      return uri;
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
          await ResetMfaAndDeleteUserAsync(email, _managementApiClient);
        }
        catch (ErrorApiException e)
        {
          if (e.ApiError.ErrorCode == "invalid_query_string")
          {
            throw new CcsSsoException("INVALID_EMAIL");
          }
        }
        catch (RateLimitApiException)
        {
          await PushDeleteUserMessageToDataQueueAsync(email);
        }
      }
    }

    public async Task<IdamUser> GetIdamUserByEmailAsync(string email)
    {
      var user = await GetIdamUserAsync(email);
      return new IdamUser()
      {
        FirstName = user.FirstName,
        LastName = user.LastName,
        EmailVerified = user.EmailVerified.HasValue ? user.EmailVerified.Value : false,
        LoginCount = !string.IsNullOrWhiteSpace(user.LoginsCount) ? int.Parse(user.LoginsCount) : 0
      };
    }

    public async Task<IdamUserInfo> GetIdamUserInfoAsync(string email)
    {
      var user = await GetIdamUserAsync(email);
      return new IdamUserInfo()
      {
        Sub = email,
        GivenName = user.FirstName,
        FamilyName = user.LastName,
        Name = user.FullName,
        NickName = user.NickName,
        Picture = user.Picture,
        UpdatedAt = user.UpdatedAt,
        Email = user.Email,
        EmailVerified = user.EmailVerified ?? false,
      };
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

    public async Task<ServiceAccessibilityResultDto> CheckServiceAccessForUserAsync(string clientId, string email)
    {
      ServiceAccessibilityResultDto response = new ServiceAccessibilityResultDto { IsAccessible = false };
      var userDetails = await GetUserAsync(email);

      if (userDetails.Detail.RolePermissionInfo.Where(rp => rp.ServiceClientId == clientId).Any() ||
        userDetails.Detail.UserGroups.Where(ugr => ugr.ServiceClientId == clientId).Any())
      {
        response.IsAccessible = true;
      }
      return response;
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
    // #Delegated
    private async Task<UserProfileInfo> GetUserAsync(string email, string delegatedOrgId = null)
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_appConfigInfo.UserExternalApiDetails.UserServiceUrl);
      httpClient.DefaultRequestHeaders.Add("X-API-Key", _appConfigInfo.UserExternalApiDetails.ApiKey);
      var requestUri = $"?user-id={HttpUtility.UrlEncode(email)}";
      if (!string.IsNullOrEmpty(delegatedOrgId))
      {
        requestUri += $"&is-delegated=true&delegated-organisation-id={HttpUtility.UrlEncode(delegatedOrgId)}";
      }
      var result = await httpClient.GetAsync(requestUri);
      var userJsonString = await result.Content.ReadAsStringAsync();
      if (!string.IsNullOrEmpty(userJsonString))
      {
        var userDetails = JsonConvert.DeserializeObject<UserProfileInfo>(userJsonString);
        return userDetails;
      }
      throw new RecordNotFoundException();
    }

    private async Task VerifyUserAccountAsync(string email)
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_appConfigInfo.UserExternalApiDetails.UserServiceUrl);
      httpClient.DefaultRequestHeaders.Add("X-API-Key", _appConfigInfo.UserExternalApiDetails.ApiKey);
      await httpClient.PutAsync($"account-verification?user-id=" + HttpUtility.UrlEncode(email), null);
    }

    private async Task<ServiceProfile> GetServiceProfileAsync(string clientId, string audience)
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_appConfigInfo.UserExternalApiDetails.ConfigurationServiceUrl);
      httpClient.DefaultRequestHeaders.Add("X-API-Key", _appConfigInfo.UserExternalApiDetails.ApiKey);
      var result = await httpClient.GetAsync($"services/{clientId}?organisation-id={audience}");
      var serviceProfileJsonString = await result.Content.ReadAsStringAsync();
      if (!string.IsNullOrEmpty(serviceProfileJsonString))
      {
        var serviceProfile = JsonConvert.DeserializeObject<ServiceProfile>(serviceProfileJsonString);
        return serviceProfile;
      }
      throw new RecordNotFoundException();
    }

    private string GetAccessToken(string clientId, string email, UserProfileInfo userDetails, string sid, string delegatedOrgId)
    {
      var rolesFromUserRoles = userDetails.Detail.RolePermissionInfo.Where(rp => rp.ServiceClientId == clientId).ToList();
      var rolesFromUserGroups = userDetails.Detail.UserGroups.Where(ug => ug.ServiceClientId == clientId).ToList();

      // #Delegated in delegated org no dashboard roles given, allow to generate token
      if ((!string.IsNullOrEmpty(delegatedOrgId) && delegatedOrgId != "0") || rolesFromUserRoles.Any() || rolesFromUserGroups.Any())
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

        // #Delegated: To handle authorization for delegate user
        if (!string.IsNullOrEmpty(delegatedOrgId) && delegatedOrgId != "0")
        {
          accesstokenClaims.Add(new ClaimInfo("roles", "DELEGATED_USER"));
        }

        accesstokenClaims.Add(new ClaimInfo("sub", email));
        var accessToken = _jwtTokenHandler.CreateToken(clientId, accesstokenClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
        return accessToken;
      }
      throw new UnauthorizedAccessException();
    }

    private string GetAccessToken(string clientId, string ciiOrgId, ServiceProfile serviceProfile)
    {
      var accesstokenClaims = new List<ClaimInfo>();
      accesstokenClaims.Add(new ClaimInfo("uid", serviceProfile.ServiceId.ToString()));
      accesstokenClaims.Add(new ClaimInfo("ciiOrgId", ciiOrgId));
      accesstokenClaims.Add(new ClaimInfo("caller", "service"));
      accesstokenClaims.Add(new ClaimInfo("sid", ""));
      foreach (var role in serviceProfile.RoleKeys)
      {
        accesstokenClaims.Add(new ClaimInfo("roles", role));
      }

      accesstokenClaims.Add(new ClaimInfo("sub", clientId));
      var accessToken = _jwtTokenHandler.CreateToken(serviceProfile.Audience, accesstokenClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
      return accessToken;
    }
    // #Delegated
    private async Task<TokenResponseInfo> GetTokensAsync(string clientId, AccessTokenResponse accessTokenResponse, string sid = null, string delegatedOrgId = null)
    {
      var tokenDecoded = _jwtTokenHandler.DecodeToken(accessTokenResponse.IdToken);

      var sidAndState = await GetSidFromState(tokenDecoded);

      if (sidAndState != null && sidAndState.Item2 != null)
      {

        sid = sidAndState.Item2;
      }

      await AttachSidWithRefreshTokenAsync(accessTokenResponse.RefreshToken, sid);
      // #Delegated
      delegatedOrgId = await MapDelegatedOrgIdWithSid(sid, delegatedOrgId);

      var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException("TOKEN_GENERATION_FAILED");
      }

      var userDetails = await GetUserAsync(email, delegatedOrgId);

      if (string.IsNullOrEmpty(delegatedOrgId))
      {
        var useMfa = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "https://ccs-sso/use_mfa")?.Value;
        if (useMfa != null)
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

        if (!userDetails.Detail.IdentityProviders.Any(idp => idp.IdentityProvider == connection))
        {
          throw new CcsSsoException("INVALID_CONNECTION");
        }

        if (!userDetails.AccountVerified)
        {
          await VerifyUserAccountAsync(email);
        }
      }

      var customClaims = GetCustomClaimsForIdToken(tokenDecoded, clientId, email, sid, userDetails);
      var idToken = _jwtTokenHandler.CreateToken(clientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);

      var accessToken = GetAccessToken(clientId, email, userDetails, sid, delegatedOrgId);

      return new TokenResponseInfo
      {
        IdToken = idToken,
        TokenType = accessTokenResponse.TokenType,
        RefreshToken = accessTokenResponse.RefreshToken,
        AccessToken = accessToken,
        ExpiresInSeconds = _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes * 60
      };
    }

    private async Task AttachSidWithRefreshTokenAsync(string refreshToken, string sid)
    {
      await _securityCacheService.SetValueAsync(refreshToken, sid, new TimeSpan(0, _appConfigInfo.SessionConfig.StateExpirationInMinutes, 0));
    }

    private async Task<Tuple<string, string>> GetSidFromState(JwtSecurityToken tokenDecoded)
    {
      var state = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "https://identify.crowncommercial.gov.uk/analytics-state")?.Value;
      string sid = null;
      if (!string.IsNullOrEmpty(state))
      {
        var sidCache = await _securityCacheService.GetValueAsync<string>(state);
        if (!string.IsNullOrEmpty(sidCache))
        {
          sid = sidCache;
        }
      }

      return new Tuple<string, string>(state, sid);
    }

    /// <summary>
    /// #Deletegated: To manage delegate user access, set/get delegatedOrgId from cache using sid 
    /// </summary>
    /// <param name="sid"></param>
    /// <param name="delegatedOrgId"></param>
    /// <returns></returns>
    private async Task<string> MapDelegatedOrgIdWithSid(string sid, string delegatedOrgId)
    {
      if (!string.IsNullOrEmpty(delegatedOrgId))
      {
        if (delegatedOrgId == "0")
        {
          // To switch user back to primary org
          await _securityCacheService.RemoveAsync(CacheKey.DELEGATION + sid);
          delegatedOrgId = null;
        }
        else
        {
          await _securityCacheService.SetValueAsync(CacheKey.DELEGATION + sid, delegatedOrgId, new TimeSpan(0, _appConfigInfo.SessionConfig.StateExpirationInMinutes, 0));
        }
      }
      else
      {
        var _delegatedOrgId = await _securityCacheService.GetValueAsync<string>(CacheKey.DELEGATION + sid);
        if (!string.IsNullOrEmpty(_delegatedOrgId))
        {
          delegatedOrgId = _delegatedOrgId;
        }
      }

      return delegatedOrgId;
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

    private async Task<User> GetIdamUserAsync(string email)
    {
      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        try
        {
          var user = (await _managementApiClient.Users.GetUsersByEmailAsync(email)).FirstOrDefault();
          if (user != null)
          {
            return user;
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

    private async Task PushCreateUserMessageToDataQueueAsync(Domain.Dtos.UserInfo userInfo)
    {
      if (_appConfigInfo.QueueInfo.EnableDataQueue)
      {
        try
        {
          SqsMessageDto sqsMessageDto = new()
          {
            MessageBody = JsonConvert.SerializeObject(userInfo),
            StringCustomAttributes = new Dictionary<string, string>
              {
                { "Destination", "Security" },
                { "Action", "POST" },
              }
          };

          await _awsDataSqsService.SendMessageAsync(_appConfigInfo.QueueInfo.DataQueueUrl, $"User-{userInfo.Email}", sqsMessageDto);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error sending message to queue. User: {userInfo.Email}, Error: {ex.Message}");
        }
      }
    }

    private async Task PushDeleteUserMessageToDataQueueAsync(string email)
    {
      if (_appConfigInfo.QueueInfo.EnableDataQueue)
      {
        try
        {
          SqsMessageDto sqsMessageDto = new()
          {
            MessageBody = email,
            StringCustomAttributes = new Dictionary<string, string>
              {
                { "Destination", "Security" },
                { "Action", "DELETE" }
              }
          };

          await _awsDataSqsService.SendMessageAsync(_appConfigInfo.QueueInfo.DataQueueUrl, $"User-{email}", sqsMessageDto);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error sending message to queue. User: {email}, Error: {ex.Message}");
        }
      }
    }

    private async Task ResetMfaAndDeleteUserAsync(string email, ManagementApiClient managementApiClient)
    {
      var users = (await managementApiClient.Users.GetUsersByEmailAsync(email)).ToList();

      if (users != null && users.Count > 0)
      {
        // While deleting user from auth0, the authenticators are not deleted from the user. 
        // when the same user is registered again, the old authenticator appears for mfa validation
        // So resetting MFA before deleting user is necessary.

        try
        {
          await ResetMfaAsync(users[0].Email);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception while resetting mfa before deleting the user from Auth0. Error Message - {ex.Message}");
        }


        foreach (var user in users)
        {
          await managementApiClient.Users.DeleteAsync(user.UserId);
        }
      }
      else
      {
        throw new RecordNotFoundException();
      }
    }
  }
}