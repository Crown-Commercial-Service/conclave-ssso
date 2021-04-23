using CcsSso.Logs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
      var _secrets = await _client.V1.Secrets.Cubbyhole.ReadSecretAsync(secretPath: "brickendon");

      var _identityProvider = _secrets.Data["IdentityProvider"].ToString();

      var _awsCognito = JsonConvert.DeserializeObject<AWSCognito>(_secrets.Data["AWSCognito"].ToString());
      var _auth0 = JsonConvert.DeserializeObject<Auth0>(_secrets.Data["Auth0"].ToString());
      var _email = JsonConvert.DeserializeObject<Email>(_secrets.Data["Email"].ToString());
      var _cors = _secrets.Data["CorsDomains"].ToString();

      if(_secrets.Data.ContainsKey("PasswordPolicy"))
      {
        var passwordPolicy = JsonConvert.DeserializeObject<PasswordPolicyVault>(_secrets.Data["PasswordPolicy"].ToString());
        Data.Add("PasswordPolicy:LowerAndUpperCaseWithDigits", passwordPolicy.LowerAndUpperCaseWithDigits.ToString());
        Data.Add("PasswordPolicy:RequiredLength", passwordPolicy.RequiredLength.ToString());
        Data.Add("PasswordPolicy:RequiredUniqueChars", passwordPolicy.RequiredUniqueChars.ToString());
      }
      

      if (_secrets.Data.ContainsKey("SessionConfig"))
      {
        var sessionConfig = JsonConvert.DeserializeObject<SessionConfigVault>(_secrets.Data["SessionConfig"].ToString());
        Data.Add("SessionConfig:SessionTimeoutInMinutes", sessionConfig.SessionTimeoutInMinutes);
      }

      if(_secrets.Data.ContainsKey("SecurityApiKeySettings"))
      {
        var securityApiKeySettings = JsonConvert.DeserializeObject<SecurityApiKeySettingsVault>(_secrets.Data["SecurityApiKeySettings"].ToString());
        Data.Add("SecurityApiKeySettings.SecurityApiKey", securityApiKeySettings.SecurityApiKey);
        Data.Add("SecurityApiKeySettings.ApiKeyValidationExcludedRoutes", string.Join(",", securityApiKeySettings.ApiKeyValidationExcludedRoutes));
      }

      if (_secrets.Data.ContainsKey("JwtTokenConfig"))
      {
        var jwtTokenInfo = JsonConvert.DeserializeObject<JwtTokenConfigVault>(_secrets.Data["JwtTokenConfig"].ToString());
        Data.Add("JwtTokenConfig:Issuer", jwtTokenInfo.Issuer);
        Data.Add("JwtTokenConfig:RsaPrivateKey", jwtTokenInfo.RsaPrivateKey);
        Data.Add("JwtTokenConfig:RsaPublicKey", jwtTokenInfo.RsaPublicKey);
        Data.Add("JwtTokenConfig:IDTokenExpirationTimeInMinutes", jwtTokenInfo.IDTokenExpirationTimeInMinutes);
        Data.Add("JwtTokenConfig:LogoutTokenExpireTimeInMinutes", jwtTokenInfo.LogoutTokenExpireTimeInMinutes);
      }

      if (_secrets.Data.ContainsKey("UserExternalApiDetails"))
      {
        var userExternalApiDetailsVault = JsonConvert.DeserializeObject<UserExternalApiDetailsVault>(_secrets.Data["UserExternalApiDetails"].ToString());
        Data.Add("UserExternalApiDetails:ApiKey", userExternalApiDetailsVault.ApiKey);
        Data.Add("UserExternalApiDetails:Url", userExternalApiDetailsVault.Url);
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

      if (_secrets.Data.ContainsKey("SecurityDbConnection"))
      {
        Data.Add("SecurityDbConnection", _secrets.Data["SecurityDbConnection"].ToString());
      }
      Data.Add("Auth0:ClientId", _auth0.ClientId);
      Data.Add("Auth0:Secret", _auth0.Secret);
      Data.Add("Auth0:Domain", _auth0.Domain);
      Data.Add("Auth0:DBConnectionName", _auth0.DBConnectionName);
      Data.Add("Auth0:ManagementApiBaseUrl", _auth0.ManagementApiBaseUrl);
      Data.Add("Auth0:ManagementApiIdentifier", _auth0.ManagementApiIdentifier);
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
      Data.Add("Email:UserActivationLinkTTLInMinutes", _email.UserActivationLinkTTLInMinutes);
      Data.Add("Email:ChangePasswordNotificationTemplateId", _email.ChangePasswordNotificationTemplateId);
      Data.Add("Email:SendNotificationsEnabled", _email.SendNotificationsEnabled);
      Data.Add("CorsDomains", _cors);
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

    public string UserActivationLinkTTLInMinutes { get; set; }

    public string ChangePasswordNotificationTemplateId { get; set; }

    public string SendNotificationsEnabled { get; set; }
  }

  public class SecurityApiKeySettingsVault
  {
    public string SecurityApiKey { get; set; }

    public string[] ApiKeyValidationExcludedRoutes { get; set; }
  }

  public class JwtTokenConfigVault
  {
    public string Issuer { get; set; }
    public string RsaPrivateKey { get; set; }
    public string RsaPublicKey { get; set; }
    public string IDTokenExpirationTimeInMinutes { get; set; }

    public string LogoutTokenExpireTimeInMinutes { get; set; }
  }

  public class SessionConfigVault
  {
    public string SessionTimeoutInMinutes { get; set; }
  }

  public class UserExternalApiDetailsVault
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
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

  public class VaultOptions
  {
    public string Address { get; set; }
  }

  public class VCapSettings
  {
    public string binding_name { get; set; }
    public Credentials credentials { get; set; }
    public Array backends { get; set; }
    public Array transit { get; set; }
    public Backend backends_shared { get; set; }
    public string instance_name { get; set; }
    public string label { get; set; }
    public string name { get; set; }
    public string plan { get; set; }
    public string provider { get; set; }
    public string syslog_drain_url { get; set; }

    public class Credentials
    {
      public string address { get; set; }
      public Auth auth { get; set; }

      public class Auth
      {
        public string accessor { get; set; }
        public string token { get; set; }
      }
    }

    public class Backend
    {
      public string application { get; set; }
      public string organization { get; set; }
      public string space { get; set; }
    }
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
}
