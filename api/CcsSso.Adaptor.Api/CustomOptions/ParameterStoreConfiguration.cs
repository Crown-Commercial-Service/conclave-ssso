using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.CustomOptions
{
  public class ParameterStoreConfigurationProvider : ConfigurationProvider
  {
    private string path = "/conclave-sso/adaptor/";
    private IAwsParameterStoreService _awsParameterStoreService;

    public ParameterStoreConfigurationProvider()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
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
      var parameters = await _awsParameterStoreService.GetParameters(path);

      var dbConnectionName = _awsParameterStoreService.FindParameterByName(parameters, path + "DbConnectionName");
      var dbConnection = _awsParameterStoreService.FindParameterByName(parameters, path + "DbConnection");

      if (!string.IsNullOrEmpty(dbConnectionName))
      {
        var dynamicDBConnection = UtilityHelper.GetDatbaseConnectionString(dbConnectionName, dbConnection);
        Data.Add("DbConnection", dynamicDBConnection);
      }
      else
      {
        Data.Add("DbConnection", dbConnection);
      }

      Data.Add("ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "ApiKey"));
      Data.Add("IsApiGatewayEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "IsApiGatewayEnabled"));
      Data.Add("InMemoryCacheExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "InMemoryCacheExpirationInMinutes"));
      Data.Add("OrganisationUserRequestPageSize", _awsParameterStoreService.FindParameterByName(parameters, path + "OrganisationUserRequestPageSize"));

      Data.Add("WrapperApiSettings:UserApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/UserApiKey"));
      Data.Add("WrapperApiSettings:OrgApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/OrgApiKey"));
      Data.Add("WrapperApiSettings:ContactApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ContactApiKey"));
      Data.Add("WrapperApiSettings:ApiGatewayEnabledUserUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayEnabledUserUrl"));
      Data.Add("WrapperApiSettings:ApiGatewayEnabledOrgUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayEnabledOrgUrl"));
      Data.Add("WrapperApiSettings:ApiGatewayEnabledContactUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayEnabledContactUrl"));
      Data.Add("WrapperApiSettings:ApiGatewayDisabledUserUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayDisabledUserUrl"));
      Data.Add("WrapperApiSettings:ApiGatewayDisabledOrgUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayDisabledOrgUrl"));
      Data.Add("WrapperApiSettings:ApiGatewayDisabledContactUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayDisabledContactUrl"));

      var redisCacheName = _awsParameterStoreService.FindParameterByName(parameters, path + "RedisCacheSettings/Name");
      var redisCacheConnectionString = _awsParameterStoreService.FindParameterByName(parameters, path + "RedisCacheSettings/ConnectionString");

      if (!string.IsNullOrEmpty(redisCacheName))
      {
        var dynamicRedisCacheConnectionString = UtilityHelper.GetRedisCacheConnectionString(redisCacheName, redisCacheConnectionString);
        Data.Add("RedisCacheSettings:ConnectionString", dynamicRedisCacheConnectionString);
      }
      else
      {
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheConnectionString);
      }

      Data.Add("RedisCacheSettings:IsEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "RedisCacheSettings/IsEnabled"));
      Data.Add("RedisCacheSettings:CacheExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "RedisCacheSettings/CacheExpirationInMinutes"));

      var queueInfoName = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/Name");

      if (!string.IsNullOrEmpty(queueInfoName))
      {
        var queueInfo = UtilityHelper.GetSqsSetting(queueInfoName);
        Data.Add("QueueInfo:AccessKeyId", queueInfo.credentials.aws_access_key_id);
        Data.Add("QueueInfo:AccessSecretKey", queueInfo.credentials.aws_secret_access_key);
      }
      else
      {
        Data.Add("QueueInfo:AccessKeyId", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AccessKeyId"));
        Data.Add("QueueInfo:AccessSecretKey", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AccessSecretKey"));
      }

      Data.Add("QueueInfo:ServiceUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/ServiceUrl"));
      Data.Add("QueueInfo:RecieveMessagesMaxCount", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/RecieveMessagesMaxCount"));
      Data.Add("QueueInfo:RecieveWaitTimeInSeconds", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/RecieveWaitTimeInSeconds"));
      Data.Add("QueueInfo:PushDataQueueUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataQueueUrl"));

      Data.Add("CiiApiSettings:Url", _awsParameterStoreService.FindParameterByName(parameters, path + "CiiApiSettings/Url"));
      Data.Add("CiiApiSettings:SpecialToken", _awsParameterStoreService.FindParameterByName(parameters, path + "CiiApiSettings/SpecialToken"));
    }

    private void GetParameterFromCommaSeparated(List<Parameter> parameters, string name, string key)
    {
      string value = _awsParameterStoreService.FindParameterByName(parameters, name);
      if (value != null)
      {
        List<string> items = value.Split(',').ToList();
        if (items != null && items.Count > 0)
        {
          int index = 0;
          foreach (var item in items)
          {
            var text = item != null ? item.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
              Data.Add($"{key}:{index++}", text);
            }
          }
        }
      }
    }
  }

  public class ParameterStoreConfigurationSource : IConfigurationSource
  {
    public ParameterStoreConfigurationSource()
    {
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
      return new ParameterStoreConfigurationProvider();
    }
  }

  public static class ParameterStoreExtensions
  {
    public static IConfigurationBuilder AddParameterStore(this IConfigurationBuilder configuration)
    {
      var parameterStoreConfigurationSource = new ParameterStoreConfigurationSource();
      configuration.Add(parameterStoreConfigurationSource);
      return configuration;
    }
  }
}
