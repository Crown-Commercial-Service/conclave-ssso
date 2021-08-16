using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  [ExcludeFromCodeCoverage]
  public class AwsIdentityProviderService : IIdentityProviderService
  {
    private readonly AmazonCognitoIdentityProviderClient _provider;
    private readonly CognitoUserPool _userPool;
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    public AwsIdentityProviderService(ApplicationConfigurationInfo appConfigInfo, IHttpClientFactory httpClientFactory)
    {
      _appConfigInfo = appConfigInfo;
      _httpClientFactory = httpClientFactory;
      //var credentials = new BasicAWSCredentials(appConfigInfo.AwsCognitoConfigurationInfo.AWSAccessKeyId, appConfigInfo.AwsCognitoConfigurationInfo.AWSAccessSecretKey);
      //_provider = new AmazonCognitoIdentityProviderClient(credentials, RegionEndpoint.GetBySystemName(appConfigInfo.AwsCognitoConfigurationInfo.AWSRegion));
      //_userPool = new CognitoUserPool(appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId, appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId, _provider);
    }

    /// <summary>
    /// Authenticates user
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="userPassword"></param>
    /// <returns></returns>
    public async Task<AuthResultDto> AuthenticateAsync(string clientId, string secret, string userName, string userPassword)
    {

      CognitoUser user = new CognitoUser(userName, _appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId, _userPool, _provider);

      InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
      {
        Password = userPassword
      };

      try
      {
        AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(authRequest);

        if (!string.IsNullOrEmpty(authResponse.ChallengeName))
        {
          return new AuthResultDto
          {
            ChallengeRequired = true,
            ChallengeName = authResponse.ChallengeName,
            SessionId = authResponse.SessionID
          };
        }

        var idToken = authResponse.AuthenticationResult.IdToken;
        var accessToken = authResponse.AuthenticationResult.AccessToken;
        var refreshToken = authResponse.AuthenticationResult.RefreshToken;

        return new AuthResultDto
        {
          IdToken = idToken,
          AccessToken = accessToken,
          RefreshToken = refreshToken
        };
      }
      catch (NotAuthorizedException)
      {
        throw new UnauthorizedAccessException();
      }
      catch (PasswordResetRequiredException)
      {
        throw new CcsSsoException("PASSWORD_RESET_REQUIRED");
      }
    }

    public async Task UpdatePendingMFAVerifiedFlagAsync(string userName, bool mfaResetVerified)
    {
      throw new NotImplementedException();
    }

      /// <summary>
      /// Creates a new User
      /// </summary>
      /// <param name="userInfo"></param>
      /// <returns></returns>
      public async Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo)
    {
      AdminCreateUserRequest adminCreateUserRequest = new AdminCreateUserRequest()
      {
        Username = userInfo.Email,
        UserPoolId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId,
        DesiredDeliveryMediums = new List<string> { "EMAIL" },
        UserAttributes = new List<AttributeType>
        {
          new AttributeType
          {
            Name = "name",
            Value = userInfo.FirstName
          },
          new AttributeType
          {
            Name = "family_name",
            Value = userInfo.LastName
          },
          new AttributeType
          {
            Name = "email",
            Value = userInfo.Email
          },
          new AttributeType()
          {
            Name = "custom:Role",
            Value = userInfo.Role
          },
          new AttributeType()
          {
            Name = "custom:Groups",
            Value = string.Join(",", userInfo.Groups)
          },
          new AttributeType
          {
            Name = "profile",
            Value = userInfo.ProfilePageUrl ?? string.Empty
          },
          new AttributeType
          {
            Name = "email_verified",
            Value = "true"
          }
        }
      };

      try
      {
        var userCreateResult = await _provider.AdminCreateUserAsync(adminCreateUserRequest);

        return new UserRegisterResult
        {
          UserName = userCreateResult.User.Username,
          UserStatus = userCreateResult.User.UserStatus
        };
      }
      catch (UsernameExistsException)
      {
        throw new CcsSsoException("USERNAME_EXISTS");
      }
    }

    public async Task UpdateUserMfaFlagAsync(Domain.Dtos.UserInfo userInfo)
    {
      throw new NotImplementedException();
    }

    public async Task ResetMfaAsync(string ticket)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Update User
    /// </summary>
    /// <param name="userInfo"></param>
    /// <returns></returns>
    public async Task UpdateUserAsync(UserInfo userInfo)
    {
      AdminUpdateUserAttributesRequest updateUserAttributesRequest = new AdminUpdateUserAttributesRequest()
      {
        Username = userInfo.UserName,
        UserPoolId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId,
        UserAttributes = new List<AttributeType>()
        {
          new AttributeType()
          {
            Name = "custom:Role",
            Value = userInfo.Role
          },
          new AttributeType()
          {
            Name = "custom:Groups",
            Value = string.Join(",", userInfo.Groups)
          }
        }
      };

      try
      {
        var userCreateResult = await _provider.AdminUpdateUserAttributesAsync(updateUserAttributesRequest);
      }
      catch (UserNotFoundException)
      {
        throw new CcsSsoException("USERNAME_NOT_EXISTS");
      }
    }

    /// <summary>
    /// The resulting refresh token will be null and cognito expects the current refresh token to
    /// be utilized until it expires. When it expires, user needs to be authenticated
    /// https://github.com/aws/aws-aspnet-cognito-identity-provider/issues/76
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<TokenResponseInfo> GetRenewedTokensAsync(string clientId, string refreshToken, string sid)
    {
      var authParam = new Dictionary<string, string>();
      authParam.Add("REFRESH_TOKEN", refreshToken);
      var req = new AdminInitiateAuthRequest()
      {
        AuthParameters = authParam,
        UserPoolId = _userPool.PoolID,
        AuthFlow = AuthFlowType.REFRESH_TOKEN,
        ClientId = _userPool.ClientID
      };

      try
      {
        var result = await _provider.AdminInitiateAuthAsync(req);
        var idToken = result.AuthenticationResult.IdToken;
        var accessToken = result.AuthenticationResult.AccessToken;
        return new TokenResponseInfo()
        {
          AccessToken = accessToken,
          RefreshToken = refreshToken
        };
      }
      catch (NotAuthorizedException)
      {
        throw new CcsSsoException("INVALID_REFRESH_TOKEN");
      }
    }

    public async Task<TokenResponseInfo> GetTokensAsync(TokenRequestInfo tokenRequestInfo, string sid)
    {
      var userPoolSettings = await GetDescribeUserPoolClientRequestAsync();
      var callBackURL = userPoolSettings.UserPoolClient.CallbackURLs.FirstOrDefault();

      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_appConfigInfo.AwsCognitoConfigurationInfo.AWSCognitoURL);
      var url = "oauth2/token";

      var list = new List<KeyValuePair<string, string>>();
      list.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
      list.Add(new KeyValuePair<string, string>("client_id", _appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId));
      list.Add(new KeyValuePair<string, string>("redirect_uri", callBackURL));
      list.Add(new KeyValuePair<string, string>("code", tokenRequestInfo.Code));

      HttpContent codeContent = new FormUrlEncodedContent(list);

      var response = await client.PostAsync(url, codeContent);
      if (response.StatusCode == System.Net.HttpStatusCode.OK)
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        var tokencontent = JsonConvert.DeserializeObject<Tokencontent>(responseContent);
        return new TokenResponseInfo()
        {
          AccessToken = tokencontent.AccessToken,
          IdToken = tokencontent.IdToken,
          RefreshToken = tokencontent.RefreshToken
        };
      }
      else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
      {
        throw new CcsSsoException("INVALID_CODE");
      }
      return null;
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// List identity providers
    /// </summary>
    /// <returns></returns>
    public async Task<List<IdentityProviderInfoDto>> ListIdentityProvidersAsync()
    {
      List<IdentityProviderInfoDto> identityProviderInfoDtos = new List<IdentityProviderInfoDto>();

      ListIdentityProvidersRequest listIdentityProvidersRequest = new ListIdentityProvidersRequest
      {
        UserPoolId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId
      };

      var result = await _provider.ListIdentityProvidersAsync(listIdentityProvidersRequest);

      foreach (var identityProvider in result.Providers)
      {
        identityProviderInfoDtos.Add(new IdentityProviderInfoDto
        {
          Name = identityProvider.ProviderName,
          Type = identityProvider.ProviderType,
          CreatedDate = identityProvider.CreationDate,
          LastModifiedDate = identityProvider.LastModifiedDate
        });
      }

      return identityProviderInfoDtos;
    }

    /// <summary>
    /// Change current password
    /// </summary>
    /// <param name="changePassword"></param>
    /// <returns></returns>
    public async Task ChangePasswordAsync(ChangePasswordDto changePassword)
    {
      ChangePasswordRequest changePasswordRequest = new ChangePasswordRequest()
      {
        // AccessToken = changePassword.AccessToken, // Removed after Auth0
        PreviousPassword = changePassword.OldPassword,
        ProposedPassword = changePassword.NewPassword
      };
      try
      {
        await _provider.ChangePasswordAsync(changePasswordRequest);
      }
      catch (NotAuthorizedException)
      {
        throw new CcsSsoException("INVALID_CREDENTIALS");
      }
      catch (InvalidParameterException)
      {
        throw new CcsSsoException("INVALID_PARAMETERS");
      }
    }

    public async Task<AuthResultDto> RespondToNewPasswordRequiredAsync(PasswordChallengeDto passwordChallengeDto)
    {
      CognitoUser user = new CognitoUser(passwordChallengeDto.UserName, _appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId, _userPool, _provider);

      RespondToNewPasswordRequiredRequest respondToNewPasswordRequiredRequest = new RespondToNewPasswordRequiredRequest
      {
        SessionID = passwordChallengeDto.SessionId,
        NewPassword = passwordChallengeDto.NewPassword
      };

      try
      {
        AuthFlowResponse authResponse = await user.RespondToNewPasswordRequiredAsync(respondToNewPasswordRequiredRequest);

        if (!string.IsNullOrEmpty(authResponse.ChallengeName))
        {
          return new AuthResultDto
          {
            ChallengeRequired = true,
            ChallengeName = authResponse.ChallengeName,
            SessionId = authResponse.SessionID
          };
        }

        var idToken = authResponse.AuthenticationResult.IdToken;
        var accessToken = authResponse.AuthenticationResult.AccessToken;
        var refreshToken = authResponse.AuthenticationResult.RefreshToken;

        return new AuthResultDto
        {
          IdToken = idToken,
          AccessToken = accessToken,
          RefreshToken = refreshToken
        };
      }
      catch (NotAuthorizedException)
      {
        throw new UnauthorizedAccessException();
      }
      catch (InvalidPasswordException)
      {
        throw new CcsSsoException("ERROR_PASSWORD_POLICY_MISMATCH");
      }
    }

    /// <summary>
    /// Initiate the reset password by sending a verification via email/sms 
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task InitiateResetPasswordAsync(ChangePasswordInitiateRequest changePasswordInitiateRequest)
    {
      AdminResetUserPasswordRequest adminResetUserPasswordRequest = new AdminResetUserPasswordRequest()
      {
        Username = changePasswordInitiateRequest.UserName,
        UserPoolId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId
      };
      await _provider.AdminResetUserPasswordAsync(adminResetUserPasswordRequest);
    }

    public string GetAuthenticationEndPoint(string state, string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt, string nonce, string display, string login_hint, int? max_age, string acr_values)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// validates verification code and reset password
    /// </summary>
    /// <param name="resetPasswordDto"></param>
    /// <returns></returns>
    public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {

      CognitoUser user = new CognitoUser(resetPasswordDto.UserName, _appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId, _userPool, _provider);

      try
      {
        await user.ConfirmForgotPasswordAsync(resetPasswordDto.VerificationCode, resetPasswordDto.NewPassword);
      }
      catch (ExpiredCodeException)
      {
        throw new CcsSsoException("INVALID_USERNAME_OR_CODE");
      }
      catch (CodeMismatchException)
      {
        throw new CcsSsoException("INVALID_CODE");
      }
      catch (InvalidParameterException)
      {
        throw new CcsSsoException("INVALID_PARAMETERS");
      }
      catch (LimitExceededException)
      {
        throw new CcsSsoException("ERROR");
      }
    }

    /// <summary>
    /// Signout from IDAM
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<string> SignOutAsync(string clientId, string userName)
    {
      try
      {
        AdminUserGlobalSignOutRequest adminUserGlobalSignOutRequest = new AdminUserGlobalSignOutRequest();
        adminUserGlobalSignOutRequest.Username = userName;
        adminUserGlobalSignOutRequest.UserPoolId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId;
        await _provider.AdminUserGlobalSignOutAsync(adminUserGlobalSignOutRequest);
      }
      catch (UserNotFoundException)
      {
        throw new CcsSsoException("USER_NOT_EXISTS");
      }
      return string.Empty;
    }


    public async Task DeleteAsync(string email)
    {
      throw new NotImplementedException();
    }

    public async Task<IdamUser> GetUser(string email)
    {
      throw new NotImplementedException();
    }

    public async Task<string> GetIdentityProviderAuthenticationEndPointAsync()
    {
      var userPoolSettings = await GetDescribeUserPoolClientRequestAsync();
      var userPoolClient = userPoolSettings.UserPoolClient;
      var responseType = userPoolClient.AllowedOAuthFlows.Any(a => a == "code") ? "code" : "token";

      var url = $"{_appConfigInfo.AwsCognitoConfigurationInfo.AWSCognitoURL}/login?client_id={_appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId}&response_type={responseType}" +
        $"&scope={string.Join("+", userPoolClient.AllowedOAuthScopes)}&redirect_uri={userPoolClient.CallbackURLs.First()}";

      return url;
    }

    private async Task<DescribeUserPoolClientResponse> GetDescribeUserPoolClientRequestAsync()
    {
      DescribeUserPoolClientRequest describeUserPoolClientRequest = new DescribeUserPoolClientRequest()
      {
        ClientId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSAppClientId,
        UserPoolId = _appConfigInfo.AwsCognitoConfigurationInfo.AWSPoolId
      };

      var userPoolSettings = await _provider.DescribeUserPoolClientAsync(describeUserPoolClientRequest);
      return userPoolSettings;
    }

    public Task<TokenResponseInfo> GetRenewedTokensAsync(string clientId, string clientSecret, string refreshToken, string sid)
    {
      throw new NotImplementedException();
    }

    public async Task SendNominateEmailAsync(Domain.Dtos.UserInfo userInfo)
    {
      throw new NotImplementedException();
    }

    public async Task SendUserActivationEmailAsync(string email, string managementApiToken = null)
    {
      throw new NotImplementedException();
    }
  }
}
