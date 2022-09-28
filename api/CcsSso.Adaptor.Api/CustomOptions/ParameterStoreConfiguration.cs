using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
using System;
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
      var configurations = new List<KeyValuePair<string, string>>();

      var dbName = _awsParameterStoreService.FindParameterByName(parameters, path + "DbName");
      var dbConnection = _awsParameterStoreService.FindParameterByName(parameters, path + "DbConnection");

      if (!string.IsNullOrEmpty(dbName))
      {
        var dynamicDBConnection = UtilityHelper.GetDatbaseConnectionString(dbName, dbConnection);
        Data.Add("DbConnection", dynamicDBConnection);
      }
      else
      {
        Data.Add("DbConnection", dbConnection);
      }

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ApiKey", "ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "IsApiGatewayEnabled", "IsApiGatewayEnabled"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "InMemoryCacheExpirationInMinutes", "InMemoryCacheExpirationInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrganisationUserRequestPageSize", "OrganisationUserRequestPageSize"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/UserApiKey", "WrapperApiSettings:UserApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/OrgApiKey", "WrapperApiSettings:OrgApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ContactApiKey", "WrapperApiSettings:ContactApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayEnabledUserUrl", "WrapperApiSettings:ApiGatewayEnabledUserUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayEnabledOrgUrl", "WrapperApiSettings:ApiGatewayEnabledOrgUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayEnabledContactUrl", "WrapperApiSettings:ApiGatewayEnabledContactUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayDisabledUserUrl", "WrapperApiSettings:ApiGatewayDisabledUserUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayDisabledOrgUrl", "WrapperApiSettings:ApiGatewayDisabledOrgUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayDisabledContactUrl", "WrapperApiSettings:ApiGatewayDisabledContactUrl"));

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

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "RedisCacheSettings/IsEnabled", "RedisCacheSettings:IsEnabled"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "RedisCacheSettings/CacheExpirationInMinutes", "RedisCacheSettings:CacheExpirationInMinutes"));

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

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/ServiceUrl", "QueueInfo:ServiceUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/RecieveMessagesMaxCount", "QueueInfo:RecieveMessagesMaxCount"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/RecieveWaitTimeInSeconds", "QueueInfo:RecieveWaitTimeInSeconds"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/PushDataQueueUrl", "QueueInfo:PushDataQueueUrl"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "CiiApiSettings/Url", "CiiApiSettings:Url"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "CiiApiSettings/SpecialToken", "CiiApiSettings:SpecialToken"));
    
      foreach (var configuration in configurations)
      {
        Data.Add(configuration);
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
