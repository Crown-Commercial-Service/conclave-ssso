using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.CustomOptions
{
  public class ParameterStoreConfigurationProvider : ConfigurationProvider
  {
    private string path = "/conclave-sso/core/";
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

      Data.Add("IsApiGatewayEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "IsApiGatewayEnabled"));
      Data.Add("EnableAdditionalLogs", _awsParameterStoreService.FindParameterByName(parameters, path + "EnableAdditionalLogs"));
      Data.Add("CustomDomain", _awsParameterStoreService.FindParameterByName(parameters, path + "CustomDomain"));
      Data.Add("DashboardServiceClientId", _awsParameterStoreService.FindParameterByName(parameters, path + "DashboardServiceClientId"));
      Data.Add("BulkUploadMaxUserCount", _awsParameterStoreService.FindParameterByName(parameters, path + "BulkUploadMaxUserCount"));

      GetParameterFromCommaSeparated(parameters, path + "CorsDomains", "CorsDomains");

      Data.Add("Cii:Url", _awsParameterStoreService.FindParameterByName(parameters, path + "Cii/Url"));
      Data.Add("Cii:Token", _awsParameterStoreService.FindParameterByName(parameters, path + "Cii/Token"));
      Data.Add("Cii:Token_Delete", _awsParameterStoreService.FindParameterByName(parameters, path + "Cii/Token_Delete"));

      Data.Add("DocUpload:Url", _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/Url"));
      Data.Add("DocUpload:Token", _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/Token"));
      Data.Add("DocUpload:SizeValidationValue", _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/SizeValidationValue"));
      Data.Add("DocUpload:TypeValidationValue", _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/TypeValidationValue"));

      Data.Add("Email:NominateEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/NominateEmailTemplateId"));
      Data.Add("Email:OrganisationJoinRequestTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/OrganisationJoinRequestTemplateId"));
      Data.Add("Email:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ApiKey"));
      Data.Add("Email:UserConfirmEmailOnlyFederatedIdpTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserConfirmEmailOnlyFederatedIdpTemplateId"));
      Data.Add("Email:UserConfirmEmailBothIdpTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserConfirmEmailBothIdpTemplateId"));
      Data.Add("Email:UserConfirmEmailOnlyUserIdPwdTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserConfirmEmailOnlyUserIdPwdTemplateId"));
      Data.Add("Email:SendNotificationsEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/SendNotificationsEnabled"));

      Data.Add("ConclaveSettings:BaseUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "ConclaveSettings/BaseUrl"));
      Data.Add("ConclaveSettings:OrgRegistrationRoute", _awsParameterStoreService.FindParameterByName(parameters, path + "ConclaveSettings/OrgRegistrationRoute"));

      Data.Add("JwtTokenValidationInfo:IdamClienId", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/IdamClienId"));
      Data.Add("JwtTokenValidationInfo:Issuer", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/Issuer"));
      Data.Add("JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/ApiGatewayEnabledJwksUrl"));
      Data.Add("JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/ApiGatewayDisabledJwksUrl"));
      Data.Add("Cii:Client_ID", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenValidationInfo/IdamClienId"));

      Data.Add("SecurityApiSettings:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/ApiKey"));
      Data.Add("SecurityApiSettings:Url", _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/Url"));

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

      var s3ConfigurationInfoName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/Name");

      if (!string.IsNullOrEmpty(s3ConfigurationInfoName))
      {
        var s3ConfigurationInfo = UtilityHelper.GetS3Settings(s3ConfigurationInfoName);
        Data.Add("S3ConfigurationInfo:AccessKeyId", s3ConfigurationInfo.credentials.aws_access_key_id);
        Data.Add("S3ConfigurationInfo:AccessSecretKey", s3ConfigurationInfo.credentials.aws_secret_access_key);
      }
      else
      {
        Data.Add("S3ConfigurationInfo:AccessKeyId", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/AccessKeyId"));
        Data.Add("S3ConfigurationInfo:AccessSecretKey", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/AccessSecretKey"));
      }

      Data.Add("S3ConfigurationInfo:ServiceUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/ServiceUrl"));
      Data.Add("S3ConfigurationInfo:BulkUploadBucketName", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/BulkUploadBucketName"));
      Data.Add("S3ConfigurationInfo:BulkUploadFolderName", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/BulkUploadFolderName"));
      Data.Add("S3ConfigurationInfo:BulkUploadTemplateFolderName", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/BulkUploadTemplateFolderName"));
      Data.Add("S3ConfigurationInfo:FileAccessExpirationInHours", _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/FileAccessExpirationInHours"));

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
