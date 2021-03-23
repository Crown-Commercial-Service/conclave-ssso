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
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

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
    /// Enable "Allow Offline Access"
    /// Enable Password grant type (Applications->Settings-> Advanced Settings->Grant Types (Password, RefreshToken)
    /// Set default connection name (Auth0 database connection name) (Profile->Settings->API Authorization Settings->Default Directory)
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="userPassword"></param>
    /// <returns></returns>
    public async Task<AuthResultDto> AuthenticateAsync(string userName, string userPassword)
    {
      try
      {
        ResourceOwnerTokenRequest resourceOwnerTokenRequest = new ResourceOwnerTokenRequest()
        {
          Username = userName,
          Password = userPassword,
          ClientId = _appConfigInfo.Auth0ConfigurationInfo.ClientId,
          ClientSecret = _appConfigInfo.Auth0ConfigurationInfo.ClientSecret,
          Scope = "offline_access" //Need this to receive a refresh token
        };

        var result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        var idToken = result.IdToken;
        var accessToken = result.AccessToken;
        var refreshToken = result.RefreshToken;

        return new AuthResultDto
        {
          IdToken = idToken,
          AccessToken = accessToken,
          RefreshToken = refreshToken
        };
      }
      catch (ErrorApiException e)
      {
        //if (e.Message.ToUpper() == "UNAUTHORIZED")
        //{
        //  throw new CcsSsoException("PASSWORD_RESET_REQUIRED");
        //}
        if (e.ApiError.Error == "invalid_grant") // This is the same error which we get for password reset required and invalid username/password
        {
          throw new CcsSsoException("INVALID_USERNAME_PASSWORD");
        }
        return null;
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
          Connection = _appConfigInfo.Auth0ConfigurationInfo.DBConnectionName
        };

        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
        {
          var result = await _managementApiClient.Users.CreateAsync(userCreateRequest);

          var ticket = await GetResetPasswordTicketAsync(userInfo.Email, managementApiToken);

          if (!string.IsNullOrEmpty(ticket))
          {
            await _ccsSsoEmailService.SendUserActivationLinkAsync(userInfo.Email, ticket);
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
        else
        {
          throw new CcsSsoException("USER_REGISTRATION_FAILED");
        }
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

    public async Task<TokenResponseInfo> GetRenewedTokensAsync(string clientId, string refreshToken)
    {
      try
      {
        RefreshTokenRequest resourceOwnerTokenRequest = new RefreshTokenRequest()
        {
          ClientId = clientId,
          RefreshToken = refreshToken,
          Scope = "email offline_access openid profile" //Need this to receive a refresh token
        };

        var result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        if (result != null)
        {
          var tokenDecoded = _jwtTokenHandler.DecodeToken(result.IdToken);
          var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
          if (string.IsNullOrEmpty(email))
          {
            throw new CcsSsoException("TOKEN_GENERATION_FAILED");
          }

          var customClaims = new List<KeyValuePair<string, string>>();
          customClaims.Add(new KeyValuePair<string, string>("email", email));
          var idToken = _jwtTokenHandler.CreateToken(clientId, customClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
          var userDetails = await GetUserAsync(email);
          var accessToken = GetAccessToken(clientId, userDetails);
          return new TokenResponseInfo
          {
            IdToken = idToken,
            RefreshToken = result.RefreshToken,
            AccessToken = accessToken
          };
        }
        throw new UnauthorizedAccessException();
      }
      catch (ErrorApiException)
      {
        throw new CcsSsoException("INVALID_CREDENTIALS");
      }
    }

    public async Task<TokenResponseInfo> GetTokensAsync(TokenRequestInfo tokenRequestInfo, string sid)
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
            Code = tokenRequestInfo.Code,
          };
          result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        }
        else
        {
          var resourceOwnerTokenRequest = new AuthorizationCodePkceTokenRequest()
          {
            ClientId = tokenRequestInfo.ClientId,
            RedirectUri = tokenRequestInfo.RedirectUrl,
            Code = tokenRequestInfo.Code,
            CodeVerifier = tokenRequestInfo.CodeVerifier
          };
          result = await _authenticationApiClient.GetTokenAsync(resourceOwnerTokenRequest);
        }
               
        if (result != null)
        {

          var tokenDecoded = _jwtTokenHandler.DecodeToken(result.IdToken);
          var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                    if (string.IsNullOrEmpty(email))
          {
            throw new CcsSsoException("TOKEN_GENERATION_FAILED");
          }

          var connection = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "http://ccs-sso/connection")?.Value;
          var userDetails = await GetUserAsync(email);
          // This will be uncommented after idp configuration is finalized
          if (userDetails.IdentityProvider != connection)
          {
            throw new CcsSsoException("INVALID_CONNECTION");
          }

          var cutomClaims = new List<KeyValuePair<string, string>>();
          cutomClaims.Add(new KeyValuePair<string, string>("email", email));
          cutomClaims.Add(new KeyValuePair<string, string>("sid", sid));
          var idToken = _jwtTokenHandler.CreateToken(tokenRequestInfo.ClientId, cutomClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);

          var accessToken = GetAccessToken(tokenRequestInfo.ClientId, userDetails);

          return new TokenResponseInfo
          {
            IdToken = idToken,
            RefreshToken = result.RefreshToken,
            AccessToken = accessToken
          };
        }
      }
      catch (ErrorApiException e)
      {
        if (e.ApiError.Error == "invalid_grant")
        {
          throw new CcsSsoException("INVALID_CODE");
        }
      }
      throw new RecordNotFoundException();
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

    public async Task InitiateResetPasswordAsync(string userName)
    {
      var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
      using (ManagementApiClient _managementApiClient = new ManagementApiClient(managementApiToken, _appConfigInfo.Auth0ConfigurationInfo.Domain))
      {
        var ticket = await GetResetPasswordTicketAsync(userName, managementApiToken);

        if (!string.IsNullOrEmpty(ticket))
        {
          await _ccsSsoEmailService.SendResetPasswordAsync(userName, ticket);
        }
        else
        {
          throw new CcsSsoException("INVALID_USER_NAME");
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

    public string GetAuthenticationEndPoint(string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt)
    {
      string uri = $"{_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl}/authorize?client_id={client_id}" +
                  $"&response_type={response_type}&scope={scope}&redirect_uri={redirect_uri}&code_challenge_method={code_challenge_method}&code_challenge={code_challenge}";
      if (!string.IsNullOrEmpty(prompt))
      {
        uri = uri + "&prompt=none";
      }
      return uri;
    }

    Task<UserClaims> IIdentityProviderService.GetUserAsync(string accessToken)
    {
      throw new NotImplementedException();
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

    private async Task<string> GetResetPasswordTicketAsync(string userName, string managementApiToken)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managementApiToken);
      client.BaseAddress = new Uri(_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl);

      var url = "/api/v2/tickets/password-change";

      var userActivationLinkTTLInSeconds = _appConfigInfo.EmailConfigurationInfo.UserActivationLinkTTLInMinutes * 60;

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
      var result = await httpClient.GetAsync($"users/?userId={email}");
      var userJsonString = await result.Content.ReadAsStringAsync();
      if (!string.IsNullOrEmpty(userJsonString))
      {
        var userDetails = JsonConvert.DeserializeObject<UserProfileInfo>(userJsonString);
        return userDetails;
      }
      throw new RecordNotFoundException();
    }

    private string GetAccessToken(string clientId, UserProfileInfo userDetails)
    {
      var roles = JsonConvert.SerializeObject(userDetails.UserGroups);
      var accesstokenClaims = new List<KeyValuePair<string, string>>();
      accesstokenClaims.Add(new KeyValuePair<string, string>("uid", userDetails.Id.ToString()));
      accesstokenClaims.Add(new KeyValuePair<string, string>("ciiOrgId", userDetails.OrganisationId));
      accesstokenClaims.Add(new KeyValuePair<string, string>("roles", roles));
      var accessToken = _jwtTokenHandler.CreateToken(clientId, accesstokenClaims, _appConfigInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);
      return accessToken;
    }
  }
}
