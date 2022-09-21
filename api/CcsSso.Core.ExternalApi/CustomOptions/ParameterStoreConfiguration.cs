using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.CustomOptions
{
  public class ParameterStoreConfigurationProvider : ConfigurationProvider
  {
    private string path = "/conclave-sso/wrapper/";
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

      GetParameterFromCommaSeparated(parameters, path + "CorsDomains", "CorsDomains");

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

      Data.Add("ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "ApiKey"));
      Data.Add("IsApiGatewayEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "IsApiGatewayEnabled"));
      Data.Add("EnableAdditionalLogs", _awsParameterStoreService.FindParameterByName(parameters, path + "EnableAdditionalLogs"));
      Data.Add("ConclaveLoginUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "ConclaveLoginUrl"));
      Data.Add("InMemoryCacheExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "InMemoryCacheExpirationInMinutes"));
      Data.Add("DashboardServiceClientId", _awsParameterStoreService.FindParameterByName(parameters, path + "DashboardServiceClientId"));

      Data.Add("JwtTokenValidationInfo:IdamClienId", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/IdamClienId"));
      Data.Add("JwtTokenValidationInfo:Issuer", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/Issuer"));
      Data.Add("JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/ApiGatewayEnabledJwksUrl"));
      Data.Add("JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/ApiGatewayDisabledJwksUrl"));
      Data.Add("Cii:Client_ID", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/IdamClienId"));

      Data.Add("SecurityApiSettings:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/ApiKey"));
      Data.Add("SecurityApiSettings:Url", _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/Url"));

      Data.Add("Email:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ApiKey"));
      Data.Add("Email:UserWelcomeEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserWelcomeEmailTemplateId"));
      Data.Add("Email:OrgProfileUpdateNotificationTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/OrgProfileUpdateNotificationTemplateId"));
      Data.Add("Email:UserContactUpdateNotificationTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserContactUpdateNotificationTemplateId"));
      Data.Add("Email:UserProfileUpdateNotificationTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserProfileUpdateNotificationTemplateId"));
      Data.Add("Email:UserPermissionUpdateNotificationTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserPermissionUpdateNotificationTemplateId"));
      Data.Add("Email:UserUpdateEmailOnlyFederatedIdpTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserUpdateEmailOnlyFederatedIdpTemplateId"));
      Data.Add("Email:UserUpdateEmailBothIdpTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserUpdateEmailBothIdpTemplateId"));
      Data.Add("Email:UserUpdateEmailOnlyUserIdPwdTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserUpdateEmailOnlyUserIdPwdTemplateId"));
      Data.Add("Email:UserConfirmEmailOnlyFederatedIdpTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserConfirmEmailOnlyFederatedIdpTemplateId"));
      Data.Add("Email:UserConfirmEmailBothIdpTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserConfirmEmailBothIdpTemplateId"));
      Data.Add("Email:UserConfirmEmailOnlyUserIdPwdTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserConfirmEmailOnlyUserIdPwdTemplateId"));
      Data.Add("Email:UserRegistrationEmailUserIdPwdTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserRegistrationEmailUserIdPwdTemplateId"));
      Data.Add("Email:UserDelegatedAccessEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserDelegatedAccessEmailTemplateId"));
      Data.Add("Email:SendNotificationsEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/SendNotificationsEnabled"));
      
      Data.Add("Cii:Url", _awsParameterStoreService.FindParameterByName(parameters, path + "Cii/Url"));
      Data.Add("Cii:Token", _awsParameterStoreService.FindParameterByName(parameters, path + "Cii/Token"));
      Data.Add("Cii:Delete_Token", _awsParameterStoreService.FindParameterByName(parameters, path + "Cii/Delete_Token"));

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
      Data.Add("QueueInfo:EnableAdaptorNotifications", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/EnableAdaptorNotifications"));
      Data.Add("QueueInfo:AdaptorNotificationQueueUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationQueueUrl"));

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

      GetParameterFromCommaSeparated(parameters, path + "ExternalServiceDefaultRoles/GlobalServiceDefaultRoles", "ExternalServiceDefaultRoles:GlobalServiceDefaultRoles");
      GetParameterFromCommaSeparated(parameters, path + "ExternalServiceDefaultRoles/ScopedServiceDefaultRoles", "ExternalServiceDefaultRoles:ScopedServiceDefaultRoles");

      Data.Add("UserDelegation:DelegationEmailExpirationHours", _awsParameterStoreService.FindParameterByName(parameters, path + "UserDelegation/DelegationEmailExpirationHours"));
      Data.Add("UserDelegation:DelegationEmailTokenEncryptionKey", _awsParameterStoreService.FindParameterByName(parameters, path + "UserDelegation/DelegationEmailTokenEncryptionKey"));
      GetParameterFromCommaSeparated(parameters, path + "UserDelegation/DelegationExcludeRoles", "UserDelegation:DelegationExcludeRoles");
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
