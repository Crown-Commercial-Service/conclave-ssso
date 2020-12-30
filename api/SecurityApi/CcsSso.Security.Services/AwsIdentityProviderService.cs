using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  [ExcludeFromCodeCoverage]
  public class AwsIdentityProviderService : IIdentityProviderService
  {
    private readonly AmazonCognitoIdentityProviderClient _provider;
    private readonly CognitoUserPool _userPool;
    private readonly ApplicationConfigurationInfo _appConfigInfo;

    public AwsIdentityProviderService(ApplicationConfigurationInfo appConfigInfo)
    {
      _appConfigInfo = appConfigInfo;
      var credentials = new BasicAWSCredentials(appConfigInfo.AWSAccessKeyId, appConfigInfo.AWSAccessSecretKey);
      _provider = new AmazonCognitoIdentityProviderClient(credentials, RegionEndpoint.GetBySystemName(appConfigInfo.AWSRegion));
      _userPool = new CognitoUserPool(appConfigInfo.AWSPoolId, appConfigInfo.AWSAppClientId, _provider);
    }

    /// <summary>
    /// Authenticates user
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="userPassword"></param>
    /// <returns></returns>
    public async Task<AuthResultDto> AuthenticateAsync(string userName, string userPassword)
    {

      CognitoUser user = new CognitoUser(userName, _appConfigInfo.AWSAppClientId, _userPool, _provider);

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
            ChallengeName = authResponse.ChallengeName
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
        UserPoolId = _appConfigInfo.AWSPoolId,
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
        UserPoolId = _appConfigInfo.AWSPoolId,
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
    public async Task<string> GetRenewedTokenAsync(string refreshToken)
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
        return idToken;
      }
      catch (NotAuthorizedException)
      {
        throw new CcsSsoException("INVALID_REFRESH_TOKEN");
      }
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
        UserPoolId = _appConfigInfo.AWSPoolId
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
        AccessToken = changePassword.AccessToken,
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

    /// <summary>
    /// Initiate the reset password by sending a verification via email/sms 
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task InitiateResetPasswordAsync(string userName)
    {
      AdminResetUserPasswordRequest adminResetUserPasswordRequest = new AdminResetUserPasswordRequest()
      {
        Username = userName,
        UserPoolId = _appConfigInfo.AWSPoolId
      };
      await _provider.AdminResetUserPasswordAsync(adminResetUserPasswordRequest);
    }

    /// <summary>
    /// validates verification code and reset password
    /// </summary>
    /// <param name="resetPasswordDto"></param>
    /// <returns></returns>
    public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {

      CognitoUser user = new CognitoUser(resetPasswordDto.UserName, _appConfigInfo.AWSAppClientId, _userPool, _provider);
      
      try
      {
        await user.ConfirmForgotPasswordAsync(resetPasswordDto.VerificationCode, resetPasswordDto.NewPassword);
      }
      catch(ExpiredCodeException)
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
    public async Task SignOutAsync(string userName)
    {
      AdminUserGlobalSignOutRequest adminUserGlobalSignOutRequest = new AdminUserGlobalSignOutRequest();
      adminUserGlobalSignOutRequest.Username = userName;
      adminUserGlobalSignOutRequest.UserPoolId = _appConfigInfo.AWSPoolId;
      await _provider.AdminUserGlobalSignOutAsync(adminUserGlobalSignOutRequest);
    }
  }
}
