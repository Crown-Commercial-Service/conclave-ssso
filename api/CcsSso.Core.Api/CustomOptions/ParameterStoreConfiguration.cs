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

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "IsApiGatewayEnabled", "IsApiGatewayEnabled"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "EnableAdditionalLogs", "EnableAdditionalLogs"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "CustomDomain", "CustomDomain"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "DashboardServiceClientId", "DashboardServiceClientId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "BulkUploadMaxUserCount", "BulkUploadMaxUserCount"));

      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "CorsDomains", "CorsDomains"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Cii/Url", "Cii:Url"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Cii/Token", "Cii:Token"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Cii/Token_Delete", "Cii:Token_Delete"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "DocUpload/Url", "DocUpload:Url"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "DocUpload/Token", "DocUpload:Token"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "DocUpload/SizeValidationValue", "DocUpload:SizeValidationValue"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "DocUpload/TypeValidationValue", "DocUpload:TypeValidationValue"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/NominateEmailTemplateId", "Email:NominateEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/OrganisationJoinRequestTemplateId", "Email:OrganisationJoinRequestTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/ApiKey", "Email:ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserConfirmEmailOnlyFederatedIdpTemplateId", "Email:UserConfirmEmailOnlyFederatedIdpTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserConfirmEmailBothIdpTemplateId", "Email:UserConfirmEmailBothIdpTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserConfirmEmailOnlyUserIdPwdTemplateId", "Email:UserConfirmEmailOnlyUserIdPwdTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/SendNotificationsEnabled", "Email:SendNotificationsEnabled"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ConclaveSettings/BaseUrl", "ConclaveSettings:BaseUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ConclaveSettings/OrgRegistrationRoute", "ConclaveSettings:OrgRegistrationRoute"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ConclaveSettings/VerifyUserDetailsRoute", "ConclaveSettings:VerifyUserDetailsRoute"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/IdamClienId", "JwtTokenValidationInfo:IdamClienId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/Issuer", "JwtTokenValidationInfo:Issuer"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/ApiGatewayEnabledJwksUrl", "JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/ApiGatewayDisabledJwksUrl", "JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/IdamClienId", "Cii:Client_ID"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SecurityApiSettings/ApiKey", "SecurityApiSettings:ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SecurityApiSettings/Url", "SecurityApiSettings:Url"));

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
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/EnableAdaptorNotifications", "QueueInfo:EnableAdaptorNotifications"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/AdaptorNotificationQueueUrl", "QueueInfo:AdaptorNotificationQueueUrl"));

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

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "S3ConfigurationInfo/ServiceUrl", "S3ConfigurationInfo:ServiceUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "S3ConfigurationInfo/BulkUploadBucketName", "S3ConfigurationInfo:BulkUploadBucketName"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "S3ConfigurationInfo/BulkUploadFolderName", "S3ConfigurationInfo:BulkUploadFolderName"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "S3ConfigurationInfo/BulkUploadTemplateFolderName", "S3ConfigurationInfo:BulkUploadTemplateFolderName"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "S3ConfigurationInfo/FileAccessExpirationInHours", "S3ConfigurationInfo:FileAccessExpirationInHours"));

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

      // #Auto validation
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidation/Enable", "OrgAutoValidation:Enable"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/OrgApiKey", "WrapperApiSettings:OrgApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayEnabledOrgUrl", "WrapperApiSettings:ApiGatewayEnabledOrgUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApiSettings/ApiGatewayDisabledOrgUrl", "WrapperApiSettings:ApiGatewayDisabledOrgUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "LookUpApiSettings/LookUpApiKey", "LookUpApiSettings:LookUpApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "LookUpApiSettings/LookUpApiUrl", "LookUpApiSettings:LookUpApiUrl"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserRoleApproval/Enable", "UserRoleApproval:Enable"));
      
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "TokenEncryptionKey", "TokenEncryptionKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "NewUserJoinRequest/LinkExpirationInMinutes", "NewUserJoinRequest:LinkExpirationInMinutes"));

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
