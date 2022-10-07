using CcsSso.Shared.Domain;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Adaptor.Api.CustomOptions
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
      var _secrets = await _client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/adaptor", mountPathValue);
      var _dbConnection = _secrets.Data["DbConnection"].ToString();
      var _apiKey = _secrets.Data["ApiKey"].ToString();
      var _isApiGatewayEnabled = _secrets.Data["IsApiGatewayEnabled"].ToString();
      var _inMemoryCacheExpirationInMinutes = _secrets.Data["InMemoryCacheExpirationInMinutes"].ToString();
      var _organisationUserRequestPageSize = _secrets.Data["OrganisationUserRequestPageSize"].ToString();

      Data.Add("DbConnection", _dbConnection);
      Data.Add("ApiKey", _apiKey);
      Data.Add("IsApiGatewayEnabled", _isApiGatewayEnabled);
      Data.Add("InMemoryCacheExpirationInMinutes", _inMemoryCacheExpirationInMinutes);
      Data.Add("OrganisationUserRequestPageSize", _organisationUserRequestPageSize);

      // Keep the trailing "/" for all the urls. Ex: "https://abc.com/user-profiles/"
      if (_secrets.Data.ContainsKey("WrapperApiSettings"))
      {
        var wrapperApiKeySettings = JsonConvert.DeserializeObject<WrapperApiSettingsVault>(_secrets.Data["WrapperApiSettings"].ToString());
        Data.Add("WrapperApiSettings:UserApiKey", wrapperApiKeySettings.UserApiKey);
        Data.Add("WrapperApiSettings:OrgApiKey", wrapperApiKeySettings.OrgApiKey);
        Data.Add("WrapperApiSettings:ContactApiKey", wrapperApiKeySettings.ContactApiKey);
        Data.Add("WrapperApiSettings:ApiGatewayEnabledUserUrl", wrapperApiKeySettings.ApiGatewayEnabledUserUrl); // Keep the trailing "/"
        Data.Add("WrapperApiSettings:ApiGatewayEnabledOrgUrl", wrapperApiKeySettings.ApiGatewayEnabledOrgUrl); // Keep the trailing "/"
        Data.Add("WrapperApiSettings:ApiGatewayEnabledContactUrl", wrapperApiKeySettings.ApiGatewayEnabledContactUrl); // Keep the trailing "/"
        Data.Add("WrapperApiSettings:ApiGatewayDisabledUserUrl", wrapperApiKeySettings.ApiGatewayDisabledUserUrl); // Keep the trailing "/"
        Data.Add("WrapperApiSettings:ApiGatewayDisabledOrgUrl", wrapperApiKeySettings.ApiGatewayDisabledOrgUrl); // Keep the trailing "/"
        Data.Add("WrapperApiSettings:ApiGatewayDisabledContactUrl", wrapperApiKeySettings.ApiGatewayDisabledContactUrl); // Keep the trailing "/"
      }

      if (_secrets.Data.ContainsKey("RedisCacheSettings"))
      {
        var redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(_secrets.Data["RedisCacheSettings"].ToString());
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheSettingsVault.ConnectionString);
        Data.Add("RedisCacheSettings:IsEnabled", redisCacheSettingsVault.IsEnabled);
        Data.Add("RedisCacheSettings:CacheExpirationInMinutes", redisCacheSettingsVault.CacheExpirationInMinutes);
      }

      if (_secrets.Data.ContainsKey("QueueInfo"))
      {
        var queueInfo = JsonConvert.DeserializeObject<QueueInfoVault>(_secrets.Data["QueueInfo"].ToString());
        Data.Add("QueueInfo:AccessKeyId", queueInfo.AccessKeyId);
        Data.Add("QueueInfo:AccessSecretKey", queueInfo.AccessSecretKey);
        Data.Add("QueueInfo:ServiceUrl", queueInfo.ServiceUrl);
        Data.Add("QueueInfo:RecieveMessagesMaxCount", queueInfo.RecieveMessagesMaxCount);
        Data.Add("QueueInfo:RecieveWaitTimeInSeconds", queueInfo.RecieveWaitTimeInSeconds);
        Data.Add("QueueInfo:PushDataQueueUrl", queueInfo.PushDataQueueUrl);
      }

      if (_secrets.Data.ContainsKey("CiiApiSettings"))
      {
        var _cii = JsonConvert.DeserializeObject<CiiVault>(_secrets.Data["CiiApiSettings"].ToString());
        Data.Add("CiiApiSettings:Url", _cii.Url);
        Data.Add("CiiApiSettings:SpecialToken", _cii.SpecialToken);
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

  public class WrapperApiSettingsVault
  {
    public string UserApiKey { get; set; }

    public string OrgApiKey { get; set; }

    public string ContactApiKey { get; set; }

    public string ApiGatewayEnabledUserUrl { get; set; }

    public string ApiGatewayEnabledOrgUrl { get; set; }

    public string ApiGatewayEnabledContactUrl { get; set; }

    public string ApiGatewayDisabledUserUrl { get; set; }

    public string ApiGatewayDisabledOrgUrl { get; set; }

    public string ApiGatewayDisabledContactUrl { get; set; }
  }

  public class RedisCacheSettingsVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }

    public string CacheExpirationInMinutes { get; set; }
  }

  public class QueueInfoVault
  {
    public string AccessKeyId { get; set; } //AWSAccessKeyId

    public string AccessSecretKey { get; set; } //AWSAccessSecretKey

    public string ServiceUrl { get; set; } //AWSServiceUrl

    public string RecieveMessagesMaxCount { get; set; }

    public string RecieveWaitTimeInSeconds { get; set; }

    public string PushDataQueueUrl { get; set; }
  }

  public class CiiVault
  {
    public string Url { get; set; }

    public string SpecialToken { get; set; }
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

