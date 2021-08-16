using System.Collections.Generic;

namespace CcsSso.Security.Domain.Dtos
{
  public class ApplicationConfigurationInfo
  {
    public AwsCognitoConfigurationInfo AwsCognitoConfigurationInfo { get; set; }

    public Auth0Configuration Auth0ConfigurationInfo { get; set; }

    public CcsEmailConfigurationInfo CcsEmailConfigurationInfo { get; set; }

    public RollBarConfigurationInfo RollBarConfigurationInfo { get; set; }

    public JwtTokenConfiguration JwtTokenConfiguration { get; set; }

    public SessionConfig SessionConfig { get; set; }

    public UserExternalApiDetails UserExternalApiDetails { get; set; }

    public PasswordPolicy PasswordPolicy { get; set; }

    public SecurityApiKeySettings SecurityApiKeySettings { get; set; }

    public RedisCacheSettings RedisCacheSettings { get; set; }

    public CryptoSettings CryptoSettings { get; set; }

    public MfaSetting MfaSetting { get; set; }
  }

  public class SecurityApiKeySettings
  {
    public string SecurityApiKey { get; set; }

    public List<string> ApiKeyValidationExcludedRoutes { get; set; }
  }

  public class RedisCacheSettings
  {
    public string ConnectionString { get; set; }

    public bool IsEnabled { get; set; }
  }

  public class CryptoSettings
  {
    public string CookieEncryptionKey { get; set; }
  }

  public class Auth0Configuration
  {
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string Domain { get; set; }

    public string DBConnectionName { get; set; }

    public string ManagementApiBaseUrl { get; set; }

    public string ManagementApiIdentifier { get; set; }

    public string DefaultDBConnectionId { get; set; }
  }

  public class AwsCognitoConfigurationInfo
  {
    public string AWSRegion { get; set; }

    public string AWSPoolId { get; set; }

    public string AWSAppClientId { get; set; }

    public string AWSAccessKeyId { get; set; }

    public string AWSAccessSecretKey { get; set; }

    public string AWSCognitoURL { get; set; }
  }

  public class CcsEmailConfigurationInfo
  {
    public string UserActivationEmailTemplateId { get; set; }

    public string ResetPasswordEmailTemplateId { get; set; }

    public string NominateEmailTemplateId { get; set; }

    public string MfaResetEmailTemplateId { get; set; }

    public string ChangePasswordNotificationTemplateId { get; set; }

    public int UserActivationLinkTTLInMinutes { get; set; }

    public int ResetPasswordLinkTTLInMinutes { get; set; }

    public bool SendNotificationsEnabled { get; set; }
  }

  public class RollBarConfigurationInfo
  {
    public string Token { get; set; }

    public string Environment { get; set; }
  }

  public class SessionConfig
  {
    public int SessionTimeoutInMinutes { get; set; }

    public int StateExpirationInMinutes { get; set; }
  }

  public class JwtTokenConfiguration
  {
    public string Issuer { get; set; }
    public string RsaPrivateKey { get; set; }
    public string RsaPublicKey { get; set; }
    public int IDTokenExpirationTimeInMinutes { get; set; }

    public int LogoutTokenExpireTimeInMinutes { get; set; }
  }

  public class UserExternalApiDetails
  {
    public string Url { get; set; }

    public string ApiKey { get; set; }
  }

  public class PasswordPolicy
  {
    public int RequiredLength { get; set; }
    public int RequiredUniqueChars { get; set; }
    public bool LowerAndUpperCaseWithDigits { get; set; }
  }

  public class MfaSetting
  {
    public int TicketExpirationInMinutes { get; set; }

    public string MfaResetRedirectUri { get; set; }

    public int MFAResetPersistentTicketListExpirationInDays { get; set; }
  }
}
