using System.Collections.Generic;
using System.Text.Json.Serialization;

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

    public WrapperApi UserExternalApiDetails { get; set; }

    public PasswordPolicy PasswordPolicy { get; set; }

    public SecurityApiKeySettings SecurityApiKeySettings { get; set; }

    public RedisCacheSettings RedisCacheSettings { get; set; }

    public CryptoSettings CryptoSettings { get; set; }

    public MfaSetting MfaSetting { get; set; }

    public MockProvider MockProvider { get; set; }

    public string CustomDomain { get; set; }

    public List<string> AllowedDomains { get; set; }

    public OpenIdConfigurationSettings OpenIdConfigurationSettings { get; set; }

    public ResetPasswordSettings ResetPasswordSettings { get; set; }
    
    public QueueInfo QueueInfo { get; set; }
  }

  public class SecurityApiKeySettings
  {
    public string SecurityApiKey { get; set; }

    public List<string> ApiKeyValidationExcludedRoutes { get; set; }

    public List<string> BearerTokenValidationIncludedRoutes { get; set; }    
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

    public string DefaultAudience { get; set; }
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

    public string JwksUrl { get; set; }

    public string IdamClienId { get; set; }
  }

  public class WrapperApi
  {
    public string ApiKey { get; set; }

    public string UserServiceUrl { get; set; }

    public string ConfigurationServiceUrl { get; set; }
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

  public class MockProvider
  {
    public string LoginUrl { get; set; }
  }

  public class OpenIdConfigurationSettings
  {
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; }

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; }

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; }
    
    [JsonPropertyName("device_authorization_endpoint")]
    public string DeviceAuthorizationEndpoint { get; set; }
    
    [JsonPropertyName("userinfo_endpoint")]
    public string UserinfoEndpoint { get; set; }
    
    [JsonPropertyName("mfa_challenge_endpoint")]
    public string MfaChallengeEndpoint { get; set; }
   
    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; }
    
    [JsonPropertyName("registration_endpoint")]
    public string RegistrationEndpoint { get; set; }
    
    [JsonPropertyName("revocation_endpoint")]
    public string RevocationEndpoint { get; set; }
    
    [JsonPropertyName("scopes_supported")]
    public List<string> ScopesSupported { get; set; }
    
    [JsonPropertyName("response_types_supported")]
    public List<string> ResponseTypesSupported { get; set; }
    
    [JsonPropertyName("code_challenge_methods_supported")]
    public List<string> CodeChallengeMethodsSupported { get; set; }
    
    [JsonPropertyName("response_modes_supported")]
    public List<string> ResponseModesSupported { get; set; }
    
    [JsonPropertyName("subject_types_supported")]
    public List<string> SubjectTypesSupported { get; set; }
    
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public List<string> IdTokenSigningAlgValuesSupported { get; set; }
    
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public List<string> TokenEndpointAuthMethodsSupported { get; set; }
    
    [JsonPropertyName("claims_supported")]
    public List<string> ClaimsSupported { get; set; }
    
    [JsonPropertyName("request_uri_parameter_supported")]
    public bool RequestUriParameterSupported { get; set; }
  }

  public class ResetPasswordSettings
  {
    public string MaxAllowedAttempts { get; set; }
    public string MaxAllowedAttemptsThresholdInMinutes { get; set; }
  }

  public class QueueInfo
  {
    public bool EnableDataQueue { get; set; }
    public string DataQueueUrl { get; set; }
  }
}
