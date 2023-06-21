using CcsSso.Logs;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Shared.Domain;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Security.Api.CustomOptions
{
  public class VaultConfigurationProvider : ConfigurationProvider
  {
    public VaultOptions _config;
    private IVaultClient _client;
    public VCapSettings _vcapSettings;

    public VaultConfigurationProvider(VaultOptions config)
    {
      _config = config;
      string env = System.Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var vault = (JObject)JsonConvert.DeserializeObject<JObject>(env)["hashicorp-vault"][0];
      _vcapSettings = JsonConvert.DeserializeObject<VCapSettings>(vault.ToString());

      IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken: _vcapSettings.credentials.auth.token);
      var vaultClientSettings = new VaultClientSettings(_vcapSettings.credentials.address, authMethod)
      {
        ContinueAsyncTasksOnCapturedContext = false
      };
      _client = new VaultClient(vaultClientSettings);
    }

    public override void Load()
    {
      LoadAsync().Wait();
      if (Data.ContainsKey("Serilog"))
      {
        LogConfigurationManager.ConfigureLogs(Data["Serilog"].ToString());
      }
    }

    public async Task LoadAsync()
    {
      await GetSecrets();
    }

    public async Task GetSecrets()
    {
      var mountPathValue = _vcapSettings.credentials.backends_shared.space.Split("/secret").FirstOrDefault();
      var _secrets = await _client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/security", mountPathValue);
      var _isApiGatewayEnabled = _secrets.Data["IsApiGatewayEnabled"].ToString();
      var _identityProvider = _secrets.Data["IdentityProvider"].ToString();

      var _awsCognito = JsonConvert.DeserializeObject<AWSCognito>(_secrets.Data["AWSCognito"].ToString());
      var _auth0 = JsonConvert.DeserializeObject<Auth0>(_secrets.Data["Auth0"].ToString());
      var _email = JsonConvert.DeserializeObject<Email>(_secrets.Data["Email"].ToString());

      Data.Add("EnableAdditionalLogs", _secrets.Data["EnableAdditionalLogs"].ToString());
      Data.Add("CustomDomain", _secrets.Data["CustomDomain"].ToString());
      if (_secrets.Data.ContainsKey("CorsDomains"))
      {
        var corsList = JsonConvert.DeserializeObject<List<string>>(_secrets.Data["CorsDomains"].ToString());
        int index = 0;
        foreach (var cors in corsList)
        {
          Data.Add($"CorsDomains:{index++}", cors);
        }
      }

      if (_secrets.Data.ContainsKey("AllowedDomains"))
      {
        var domainList = JsonConvert.DeserializeObject<List<string>>(_secrets.Data["AllowedDomains"].ToString());
        int index = 0;
        foreach (var domain in domainList)
        {
          Data.Add($"AllowedDomains:{index++}", domain);
        }
      }

      if (_secrets.Data.ContainsKey("PasswordPolicy"))
      {
        var passwordPolicy = JsonConvert.DeserializeObject<PasswordPolicyVault>(_secrets.Data["PasswordPolicy"].ToString());
        Data.Add("PasswordPolicy:LowerAndUpperCaseWithDigits", passwordPolicy.LowerAndUpperCaseWithDigits.ToString());
        Data.Add("PasswordPolicy:RequiredLength", passwordPolicy.RequiredLength.ToString());
        Data.Add("PasswordPolicy:RequiredUniqueChars", passwordPolicy.RequiredUniqueChars.ToString());
      }

      if (_secrets.Data.ContainsKey("RedisCacheSettings"))
      {
        var redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(_secrets.Data["RedisCacheSettings"].ToString());
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheSettingsVault.ConnectionString);
        Data.Add("RedisCacheSettings:IsEnabled", redisCacheSettingsVault.IsEnabled);
      }

      if (_secrets.Data.ContainsKey("SessionConfig"))
      {
        var sessionConfig = JsonConvert.DeserializeObject<SessionConfigVault>(_secrets.Data["SessionConfig"].ToString());
        Data.Add("SessionConfig:SessionTimeoutInMinutes", sessionConfig.SessionTimeoutInMinutes);
        Data.Add("SessionConfig:StateExpirationInMinutes", sessionConfig.StateExpirationInMinutes);
      }

      if (_secrets.Data.ContainsKey("SecurityApiKeySettings"))
      {
        var securityApiKeySettings = JsonConvert.DeserializeObject<SecurityApiKeySettingsVault>(_secrets.Data["SecurityApiKeySettings"].ToString());
        Data.Add("SecurityApiKeySettings:SecurityApiKey", securityApiKeySettings.SecurityApiKey);
        int index = 0;
        foreach (var route in securityApiKeySettings.ApiKeyValidationExcludedRoutes)
        {
          Data.Add($"SecurityApiKeySettings:ApiKeyValidationExcludedRoutes:{index++}", route);
        }
        int tokenIndex = 0;
        foreach (var route in securityApiKeySettings.BearerTokenValidationIncludedRoutes)
        {
          Data.Add($"SecurityApiKeySettings:BearerTokenValidationIncludedRoutes:{tokenIndex++}", route);
        }
      }

      if (_secrets.Data.ContainsKey("JwtTokenConfig"))
      {
        var jwtTokenInfo = JsonConvert.DeserializeObject<JwtTokenConfigVault>(_secrets.Data["JwtTokenConfig"].ToString());
        Data.Add("JwtTokenConfig:Issuer", jwtTokenInfo.Issuer);
        Data.Add("JwtTokenConfig:RsaPrivateKey", jwtTokenInfo.RsaPrivateKey);
        Data.Add("JwtTokenConfig:RsaPublicKey", jwtTokenInfo.RsaPublicKey);
        Data.Add("JwtTokenConfig:IDTokenExpirationTimeInMinutes", jwtTokenInfo.IDTokenExpirationTimeInMinutes);
        Data.Add("JwtTokenConfig:LogoutTokenExpireTimeInMinutes", jwtTokenInfo.LogoutTokenExpireTimeInMinutes);
        Data.Add("JwtTokenConfig:JwksUrl", jwtTokenInfo.JwksUrl);
        Data.Add("JwtTokenConfig:IdamClienId", jwtTokenInfo.IdamClienId);
      }

      // Keep the trailing "/" for all the urls. Ex: "https://abc.com/user-profiles/"
      if (_secrets.Data.ContainsKey("WrapperApi"))
      {
        var wrapperApiVault = JsonConvert.DeserializeObject<WrapperApiVault>(_secrets.Data["WrapperApi"].ToString());
        Data.Add("WrapperApi:ApiKey", wrapperApiVault.ApiKey);
        Data.Add("WrapperApi:UserServiceUrl", wrapperApiVault.UserServiceUrl);
        Data.Add("WrapperApi:ConfigurationServiceUrl", wrapperApiVault.ConfigurationServiceUrl);
      }

      if (_secrets.Data.ContainsKey("RollBarLogger"))
      {
        var rollBarSettings = JsonConvert.DeserializeObject<RollBarLogger>(_secrets.Data["RollBarLogger"].ToString());
        Data.Add("RollBarLogger:Token", rollBarSettings.Token);
        Data.Add("RollBarLogger:Environment", rollBarSettings.Environment);
      }

      if (_secrets.Data.ContainsKey("Serilog"))
      {
        Data.Add("Serilog", _secrets.Data["Serilog"].ToString());
      }

      if (_secrets.Data.ContainsKey("MockProvider"))
      {
        var mockProvider = JsonConvert.DeserializeObject<MockProvider>(_secrets.Data["MockProvider"].ToString());
        Data.Add("MockProvider:LoginUrl", mockProvider.LoginUrl);
      }

      if (_secrets.Data.ContainsKey("SecurityDbConnection"))
      {
        Data.Add("SecurityDbConnection", _secrets.Data["SecurityDbConnection"].ToString());
      }

      if (_secrets.Data.ContainsKey("Crypto"))
      {
        var cryptoVault = JsonConvert.DeserializeObject<CryptoVault>(_secrets.Data["Crypto"].ToString());
        Data.Add("Crypto:CookieEncryptionKey", cryptoVault.CookieEncryptionKey);
      }

      if (_secrets.Data.ContainsKey("MfaSettings"))
      {
        var mfaSettingVault = JsonConvert.DeserializeObject<MfaSettingVault>(_secrets.Data["MfaSettings"].ToString());
        Data.Add("MfaSettings:TicketExpirationInMinutes", mfaSettingVault.TicketExpirationInMinutes);
        Data.Add("MfaSettings:MfaResetRedirectUri", mfaSettingVault.MfaResetRedirectUri);
        Data.Add("MfaSettings:MFAResetPersistentTicketListExpirationInDays", mfaSettingVault.MFAResetPersistentTicketListExpirationInDays);
      }

      if (_secrets.Data.ContainsKey("OpenIdConfigurationSettings"))
      {
        var openIdConfigurationSettings = JsonConvert.DeserializeObject<OpenIdConfigurationSettingsVault>(_secrets.Data["OpenIdConfigurationSettings"].ToString());
        Data.Add("OpenIdConfigurationSettings:Issuer", openIdConfigurationSettings.Issuer);
        Data.Add("OpenIdConfigurationSettings:AuthorizationEndpoint", openIdConfigurationSettings.AuthorizationEndpoint);
        Data.Add("OpenIdConfigurationSettings:TokenEndpoint", openIdConfigurationSettings.TokenEndpoint);
        Data.Add("OpenIdConfigurationSettings:DeviceAuthorizationEndpoint", openIdConfigurationSettings.DeviceAuthorizationEndpoint);
        Data.Add("OpenIdConfigurationSettings:UserinfoEndpoint", openIdConfigurationSettings.UserinfoEndpoint);
        Data.Add("OpenIdConfigurationSettings:MfaChallengeEndpoint", openIdConfigurationSettings.MfaChallengeEndpoint);
        Data.Add("OpenIdConfigurationSettings:JwksUri", openIdConfigurationSettings.JwksUri);
        Data.Add("OpenIdConfigurationSettings:RegistrationEndpoint", openIdConfigurationSettings.RegistrationEndpoint);
        Data.Add("OpenIdConfigurationSettings:RevocationEndpoint", openIdConfigurationSettings.RevocationEndpoint);
        int i = 0;
        foreach (var route in openIdConfigurationSettings.ScopesSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:ScopesSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.ResponseTypesSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:ResponseTypesSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.CodeChallengeMethodsSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:CodeChallengeMethodsSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.ResponseModesSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:ResponseModesSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.SubjectTypesSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:SubjectTypesSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.IdTokenSigningAlgValuesSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:IdTokenSigningAlgValuesSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.TokenEndpointAuthMethodsSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:TokenEndpointAuthMethodsSupported:{i++}", route);
        }
        i = 0;
        foreach (var route in openIdConfigurationSettings.ClaimsSupported)
        {
          Data.Add($"OpenIdConfigurationSettings:ClaimsSupported:{i++}", route);
        }
        Data.Add("OpenIdConfigurationSettings:RequestUriParameterSupported", openIdConfigurationSettings.RequestUriParameterSupported.ToString());
      }

      if (_secrets.Data.ContainsKey("ResetPasswordSettings"))
      {
        var resetPasswordSettings = JsonConvert.DeserializeObject<ResetPasswordSettings>(_secrets.Data["ResetPasswordSettings"].ToString());
        Data.Add("ResetPasswordSettings:MaxAllowedAttempts", resetPasswordSettings.MaxAllowedAttempts);
        Data.Add("ResetPasswordSettings:MaxAllowedAttemptsThresholdInMinutes", resetPasswordSettings.MaxAllowedAttemptsThresholdInMinutes);
      }

      Data.Add("IsApiGatewayEnabled", _isApiGatewayEnabled);
      Data.Add("Auth0:ClientId", _auth0.ClientId);
      Data.Add("Auth0:Secret", _auth0.Secret);
      Data.Add("Auth0:Domain", _auth0.Domain);
      Data.Add("Auth0:DBConnectionName", _auth0.DBConnectionName);
      Data.Add("Auth0:ManagementApiBaseUrl", _auth0.ManagementApiBaseUrl);
      Data.Add("Auth0:ManagementApiIdentifier", _auth0.ManagementApiIdentifier);
      Data.Add("Auth0:DefaultAudience", _auth0.DefaultAudience);

      Data.Add("Auth0:UserStore", _auth0.UserStore);
      Data.Add("Auth0:DefaultDBConnectionId", _auth0.DefaultDBConnectionId);
      Data.Add("AWSCognito:Region", _awsCognito.Region);
      Data.Add("AWSCognito:PoolId", _awsCognito.PoolId);
      Data.Add("AWSCognito:AppClientId", _awsCognito.AppClientId);
      Data.Add("AWSCognito:AccessKeyId", _awsCognito.AccessKeyId);
      Data.Add("AWSCognito:AccessSecretKey", _awsCognito.AccessSecretKey);
      Data.Add("AWSCognito:AWSCognitoURL", _awsCognito.AWSCognitoURL);
      Data.Add("IdentityProvider", _identityProvider);
      Data.Add("Email:ApiKey", _email.ApiKey);
      Data.Add("Email:UserActivationEmailTemplateId", _email.UserActivationEmailTemplateId);
      Data.Add("Email:ResetPasswordEmailTemplateId", _email.ResetPasswordEmailTemplateId);
      Data.Add("Email:NominateEmailTemplateId", _email.NominateEmailTemplateId);
      Data.Add("Email:MfaResetEmailTemplateId", _email.MfaResetEmailTemplateId);
      Data.Add("Email:UserActivationLinkTTLInMinutes", _email.UserActivationLinkTTLInMinutes);
      Data.Add("Email:ChangePasswordNotificationTemplateId", _email.ChangePasswordNotificationTemplateId);
      Data.Add("Email:ResetPasswordLinkTTLInMinutes", _email.ResetPasswordLinkTTLInMinutes);
      Data.Add("Email:SendNotificationsEnabled", _email.SendNotificationsEnabled);

      if (_secrets.Data.ContainsKey("QueueInfo"))
      {
        var queueInfo = JsonConvert.DeserializeObject<QueueInfoVault>(_secrets.Data["QueueInfo"].ToString());
        Data.Add("QueueInfo:ServiceUrl", queueInfo.ServiceUrl);
        Data.Add("QueueInfo:EnableDataQueue", queueInfo.EnableDataQueue);
        Data.Add("QueueInfo:DataQueueAccessKeyId", queueInfo.DataQueueAccessKeyId);
        Data.Add("QueueInfo:DataQueueAccessSecretKey", queueInfo.DataQueueAccessSecretKey);
        Data.Add("QueueInfo:DataQueueUrl", queueInfo.DataQueueUrl);
        Data.Add("QueueInfo:DataQueueRecieveMessagesMaxCount", queueInfo.DataQueueRecieveMessagesMaxCount);
        Data.Add("QueueInfo:DataQueueRecieveWaitTimeInSeconds", queueInfo.DataQueueRecieveWaitTimeInSeconds);
      }
    }
  }

  public class VaultConfigurationSource : IConfigurationSource
  {
    private VaultOptions _config;

    public VaultConfigurationSource(Action<VaultOptions> config)
    {
      _config = new VaultOptions();
      config.Invoke(_config);
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
      return new VaultConfigurationProvider(_config);
    }
  }

  public class Auth0
  {
    public string ClientId { get; set; }
    public string Secret { get; set; }
    public string Domain { get; set; }
    public string DBConnectionName { get; set; }
    public string ManagementApiBaseUrl { get; set; }
    public string ManagementApiIdentifier { get; set; }
    public string UserStore { get; set; }
    public string DefaultDBConnectionId { get; set; }
    public string DefaultAudience { get; set; }
  }

  public class AWSCognito
  {
    public string Region { get; set; }
    public string PoolId { get; set; }
    public string AppClientId { get; set; }
    public string AccessKeyId { get; set; }
    public string AccessSecretKey { get; set; }
    public string AWSCognitoURL { get; set; }
  }

  public class Email
  {
    public string ApiKey { get; set; }

    public string UserActivationEmailTemplateId { get; set; }

    public string ResetPasswordEmailTemplateId { get; set; }

    public string NominateEmailTemplateId { get; set; }

    public string MfaResetEmailTemplateId { get; set; }

    public string UserActivationLinkTTLInMinutes { get; set; }

    public string ResetPasswordLinkTTLInMinutes { get; set; }

    public string ChangePasswordNotificationTemplateId { get; set; }

    public string SendNotificationsEnabled { get; set; }
  }

  public class SecurityApiKeySettingsVault
  {
    public string SecurityApiKey { get; set; }

    public string[] ApiKeyValidationExcludedRoutes { get; set; }

    public string[] BearerTokenValidationIncludedRoutes { get; set; }
  }

  public class JwtTokenConfigVault
  {
    public string Issuer { get; set; }

    public string RsaPrivateKey { get; set; }

    public string RsaPublicKey { get; set; }

    public string IDTokenExpirationTimeInMinutes { get; set; }

    public string LogoutTokenExpireTimeInMinutes { get; set; }

    public string JwksUrl { get; set; }

    public string IdamClienId { get; set; }
  }

  public class SessionConfigVault
  {
    public string SessionTimeoutInMinutes { get; set; }

    public string StateExpirationInMinutes { get; set; }
  }

  public class WrapperApiVault
  {
    public string ApiKey { get; set; }

    public string UserServiceUrl { get; set; }

    public string ConfigurationServiceUrl { get; set; }
  }

  public class PasswordPolicyVault
  {
    public int RequiredLength { get; set; }
    public int RequiredUniqueChars { get; set; }
    public bool LowerAndUpperCaseWithDigits { get; set; }
  }

  public class RollBarLogger
  {
    public string Environment { get; set; }

    public string Token { get; set; }
  }

  public class RedisCacheSettingsVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }
  }

  public class CryptoVault
  {
    public string CookieEncryptionKey { get; set; }
  }

  public class MfaSettingVault
  {
    public string TicketExpirationInMinutes { get; set; }

    public string MfaResetRedirectUri { get; set; }

    public string MFAResetPersistentTicketListExpirationInDays { get; set; }
  }

  public class VaultOptions
  {
    public string Address { get; set; }
  }

  public static class VaultExtensions
  {
    public static IConfigurationBuilder AddVault(this IConfigurationBuilder configuration,
    Action<VaultOptions> options)
    {
      var vaultOptions = new VaultConfigurationSource(options);
      configuration.Add(vaultOptions);
      return configuration;
    }
  }

  public class OpenIdConfigurationSettingsVault
  {
    public string Issuer { get; set; }

    public string AuthorizationEndpoint { get; set; }

    public string TokenEndpoint { get; set; }

    public string DeviceAuthorizationEndpoint { get; set; }

    public string UserinfoEndpoint { get; set; }

    public string MfaChallengeEndpoint { get; set; }

    public string JwksUri { get; set; }

    public string RevocationEndpoint { get; set; }

    public string RegistrationEndpoint { get; set; }

    public string[] ScopesSupported { get; set; }

    public string[] ResponseTypesSupported { get; set; }

    public string[] CodeChallengeMethodsSupported { get; set; }

    public string[] ResponseModesSupported { get; set; }

    public string[] SubjectTypesSupported { get; set; }

    public string[] IdTokenSigningAlgValuesSupported { get; set; }

    public string[] TokenEndpointAuthMethodsSupported { get; set; }

    public string[] ClaimsSupported { get; set; }

    public string RequestUriParameterSupported { get; set; }
  }

  public class QueueInfoVault
  {
    public string ServiceUrl { get; set; } 

    public string EnableDataQueue { get; set; }

    public string DataQueueAccessKeyId { get; set; }

    public string DataQueueAccessSecretKey { get; set; }

    public string DataQueueUrl { get; set; }

    public string DataQueueRecieveMessagesMaxCount { get; set; }

    public string DataQueueRecieveWaitTimeInSeconds { get; set; }
  }
}

