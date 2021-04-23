using System;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;
using Microsoft.Extensions.Configuration;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CcsSso.ExternalApi.Api.CustomOptions
{
  public class VaultConfigurationProvider : ConfigurationProvider
  {
    public VaultOptions _config;
    private IVaultClient _client;
    public VCapSettings _vcapSettings;

    public VaultConfigurationProvider(VaultOptions config)
    {
      _config = config;

      var env = System.Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      Console.WriteLine(env);
      var vault = (JObject)JsonConvert.DeserializeObject<JObject>(env)["hashicorp-vault"][0];
      _vcapSettings = JsonConvert.DeserializeObject<VCapSettings>(vault.ToString());

      IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken: _vcapSettings.credentials.auth.token);
      var vaultClientSettings = new VaultClientSettings("https://dev.vault.ai-cloud.uk:8443", authMethod)
      {
        ContinueAsyncTasksOnCapturedContext = false
      };
      _client = new VaultClient(vaultClientSettings);
    }

    public override void Load()
    {
      LoadAsync().Wait();
    }

    public async Task LoadAsync()
    {
      await GetSecrets();
    }

    public async Task GetSecrets()
    {
      var _secrets = await _client.V1.Secrets.Cubbyhole.ReadSecretAsync(secretPath: "brickendon/core");
      var _dbConnection = _secrets.Data["DbConnection"].ToString();
      var _key = _secrets.Data["ApiKey"].ToString();
      var _cors = _secrets.Data["CorsDomains"].ToString(); 
      var _conclaveLoginUrl = _secrets.Data["ConclaveLoginUrl"].ToString(); 

      Data.Add("DbConnection", _dbConnection);
      Data.Add("ApiKey", _key);
      Data.Add("CorsDomains", _cors);
      Data.Add("ConclaveLoginUrl", _conclaveLoginUrl);

      if (_secrets.Data.ContainsKey("JwtTokenValidationInfo"))
      {
        var jwtTokenValidationInfoVault = JsonConvert.DeserializeObject<JwtTokenValidationInfoVault>(_secrets.Data["JwtTokenValidationInfo"].ToString());
        Data.Add("JwtTokenValidationInfo:IdamClienId", jwtTokenValidationInfoVault.IdamClienId);
        Data.Add("JwtTokenValidationInfo:Issuer", jwtTokenValidationInfoVault.Issuer);
        Data.Add("JwtTokenValidationInfo:JwksUrl", jwtTokenValidationInfoVault.JwksUrl);
      }

      if (_secrets.Data.ContainsKey("SecurityApiSettings"))
      {
        var securityApiKeySettings = JsonConvert.DeserializeObject<SecurityApiSettingsVault>(_secrets.Data["SecurityApiSettings"].ToString());
        Data.Add("SecurityApiSettings:ApiKey", securityApiKeySettings.ApiKey);
        Data.Add("SecurityApiSettings:Url", securityApiKeySettings.Url);
      }

      if (_secrets.Data.ContainsKey("Email"))
      {
        var emailsettings = JsonConvert.DeserializeObject<Email>(_secrets.Data["Email"].ToString());
        Data.Add("Email:ApiKey", emailsettings.ApiKey);
        Data.Add("Email:UserWelcomeEmailTemplateId", emailsettings.UserWelcomeEmailTemplateId);
        Data.Add("Email:OrgProfileUpdateNotificationTemplateId", emailsettings.OrgProfileUpdateNotificationTemplateId);
        Data.Add("Email:UserContactUpdateNotificationTemplateId", emailsettings.UserContactUpdateNotificationTemplateId);
        Data.Add("Email:UserProfileUpdateNotificationTemplateId", emailsettings.UserProfileUpdateNotificationTemplateId);
        Data.Add("Email:UserPermissionUpdateNotificationTemplateId", emailsettings.UserPermissionUpdateNotificationTemplateId);
        Data.Add("Email:SendNotificationsEnabled", emailsettings.SendNotificationsEnabled);
      }

      if (_secrets.Data.ContainsKey("Cii"))
      {
        var _cii = JsonConvert.DeserializeObject<Cii>(_secrets.Data["Cii"].ToString());
        Data.Add("Cii:Url", _cii.Url);
        Data.Add("Cii:Token", _cii.Token);
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

  public class JwtTokenValidationInfoVault
  {
    public string IdamClienId { get; set; }

    public string Issuer { get; set; }

    public string JwksUrl { get; set; }
  }

  public class SecurityApiSettingsVault
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class Email
  {
    public string ApiKey { get; set; }

    public string UserWelcomeEmailTemplateId { get; set; }

    public string OrgProfileUpdateNotificationTemplateId { get; set; }

    public string UserProfileUpdateNotificationTemplateId { get; set; }

    public string UserContactUpdateNotificationTemplateId { get; set; }

    public string UserPermissionUpdateNotificationTemplateId { get; set; }

    public string SendNotificationsEnabled { get; set; }
  }

  public class Cii
  {
    public string Url { get; set; }
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
