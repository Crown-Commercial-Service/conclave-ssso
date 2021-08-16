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
using System.Linq;
using CcsSso.Shared.Domain;

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
      var vaultClientSettings = new VaultClientSettings(_vcapSettings.credentials.address, authMethod)
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
      var mountPathValue = _vcapSettings.credentials.backends_shared.space.Split("/secret").FirstOrDefault();
      var _secrets = await _client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/wrapper", mountPathValue);
      var _dbConnection = _secrets.Data["DbConnection"].ToString();
      var _key = _secrets.Data["ApiKey"].ToString();
      var _isApiGatewayEnabled = _secrets.Data["IsApiGatewayEnabled"].ToString();

      if (_secrets.Data.ContainsKey("CorsDomains"))
      {
        var corsList = JsonConvert.DeserializeObject<List<string>>(_secrets.Data["CorsDomains"].ToString());
        int index = 0;
        foreach (var cors in corsList)
        {
          Data.Add($"CorsDomains:{index++}", cors);
        }
      }
      var _conclaveLoginUrl = _secrets.Data["ConclaveLoginUrl"].ToString(); 
      var _inMemoryCacheExpirationInMinutes = _secrets.Data["InMemoryCacheExpirationInMinutes"].ToString(); 
      var _dashboardServiceClientId = _secrets.Data["DashboardServiceClientId"].ToString();

      Data.Add("DbConnection", _dbConnection);
      Data.Add("ApiKey", _key);
      Data.Add("IsApiGatewayEnabled", _isApiGatewayEnabled);
      Data.Add("ConclaveLoginUrl", _conclaveLoginUrl);
      Data.Add("InMemoryCacheExpirationInMinutes", _inMemoryCacheExpirationInMinutes);
      Data.Add("DashboardServiceClientId", _dashboardServiceClientId);

      if (_secrets.Data.ContainsKey("JwtTokenValidationInfo"))
      {
        var jwtTokenValidationInfoVault = JsonConvert.DeserializeObject<JwtTokenValidationInfoVault>(_secrets.Data["JwtTokenValidationInfo"].ToString());
        Data.Add("JwtTokenValidationInfo:IdamClienId", jwtTokenValidationInfoVault.IdamClienId);
        Data.Add("JwtTokenValidationInfo:Issuer", jwtTokenValidationInfoVault.Issuer);
        Data.Add("JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl", jwtTokenValidationInfoVault.ApiGatewayEnabledJwksUrl);
        Data.Add("JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl", jwtTokenValidationInfoVault.ApiGatewayDisabledJwksUrl);
        Data.Add("Cii:Client_ID", jwtTokenValidationInfoVault.IdamClienId);
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
        Data.Add("Cii:Url", _cii.url);
        Data.Add("Cii:Token", _cii.token);
        Data.Add("Cii:Delete_Token", _cii.token_delete);
      }

      if (_secrets.Data.ContainsKey("QueueInfo"))
      {
        var queueInfo = JsonConvert.DeserializeObject<QueueInfoVault>(_secrets.Data["QueueInfo"].ToString());
        Data.Add("QueueInfo:AccessKeyId", queueInfo.AccessKeyId);
        Data.Add("QueueInfo:AccessSecretKey", queueInfo.AccessSecretKey); 
        Data.Add("QueueInfo:ServiceUrl", queueInfo.ServiceUrl);
        Data.Add("QueueInfo:RecieveMessagesMaxCount", queueInfo.RecieveMessagesMaxCount);
        Data.Add("QueueInfo:RecieveWaitTimeInSeconds", queueInfo.RecieveWaitTimeInSeconds);
        Data.Add("QueueInfo:EnableAdaptorNotifications", queueInfo.EnableAdaptorNotifications);
        Data.Add("QueueInfo:AdaptorNotificationQueueUrl", queueInfo.AdaptorNotificationQueueUrl);
      }

      if (_secrets.Data.ContainsKey("RedisCacheSettings"))
      {
        var redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(_secrets.Data["RedisCacheSettings"].ToString());
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheSettingsVault.ConnectionString);
        Data.Add("RedisCacheSettings:IsEnabled", redisCacheSettingsVault.IsEnabled);
        Data.Add("RedisCacheSettings:CacheExpirationInMinutes", redisCacheSettingsVault.CacheExpirationInMinutes);
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

    public string ApiGatewayEnabledJwksUrl { get; set; }

    public string ApiGatewayDisabledJwksUrl { get; set; }
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
    public string url { get; set; }
    public string token { get; set; }
    public string token_delete { get; set; }
    public string client_id { get; set; }
  }

  public class QueueInfoVault
  {
    public string AccessKeyId { get; set; } //AWSAccessKeyId

    public string AccessSecretKey { get; set; } //AWSAccessSecretKey

    public string ServiceUrl { get; set; } //AWSServiceUrl

    public string RecieveMessagesMaxCount { get; set; }

    public string RecieveWaitTimeInSeconds { get; set; }

    public string EnableAdaptorNotifications { get; set; }

    public string AdaptorNotificationQueueUrl { get; set; }
  }

  public class RedisCacheSettingsVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }

    public string CacheExpirationInMinutes { get; set; }
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
}
