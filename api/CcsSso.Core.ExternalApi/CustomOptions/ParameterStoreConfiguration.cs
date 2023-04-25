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
      var configurations = new List<KeyValuePair<string, string>>();

      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "CorsDomains", "CorsDomains"));

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
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "EnableAdditionalLogs", "EnableAdditionalLogs"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "EnableUserAccessTokenFix", "EnableUserAccessTokenFix"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ConclaveLoginUrl", "ConclaveLoginUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "InMemoryCacheExpirationInMinutes", "InMemoryCacheExpirationInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "DashboardServiceClientId", "DashboardServiceClientId"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/IdamClienId", "JwtTokenValidationInfo:IdamClienId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/Issuer", "JwtTokenValidationInfo:Issuer"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/ApiGatewayEnabledJwksUrl", "JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/ApiGatewayDisabledJwksUrl", "JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenValidationInfo/IdamClienId", "Cii:Client_ID"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SecurityApiSettings/ApiKey", "SecurityApiSettings:ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SecurityApiSettings/Url", "SecurityApiSettings:Url"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/ApiKey", "Email:ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserWelcomeEmailTemplateId", "Email:UserWelcomeEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/OrgProfileUpdateNotificationTemplateId", "Email:OrgProfileUpdateNotificationTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserContactUpdateNotificationTemplateId", "Email:UserContactUpdateNotificationTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserProfileUpdateNotificationTemplateId", "Email:UserProfileUpdateNotificationTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserPermissionUpdateNotificationTemplateId", "Email:UserPermissionUpdateNotificationTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserUpdateEmailOnlyFederatedIdpTemplateId", "Email:UserUpdateEmailOnlyFederatedIdpTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserUpdateEmailBothIdpTemplateId", "Email:UserUpdateEmailBothIdpTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserUpdateEmailOnlyUserIdPwdTemplateId", "Email:UserUpdateEmailOnlyUserIdPwdTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserConfirmEmailOnlyFederatedIdpTemplateId", "Email:UserConfirmEmailOnlyFederatedIdpTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserConfirmEmailBothIdpTemplateId", "Email:UserConfirmEmailBothIdpTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserConfirmEmailOnlyUserIdPwdTemplateId", "Email:UserConfirmEmailOnlyUserIdPwdTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserRegistrationEmailUserIdPwdTemplateId", "Email:UserRegistrationEmailUserIdPwdTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserDelegatedAccessEmailTemplateId", "Email:UserDelegatedAccessEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/SendNotificationsEnabled", "Email:SendNotificationsEnabled"));
      
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Cii/Url", "Cii:Url"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Cii/Token", "Cii:Token"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Cii/Delete_Token", "Cii:Delete_Token"));

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

      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "ExternalServiceDefaultRoles/GlobalServiceDefaultRoles", "ExternalServiceDefaultRoles:GlobalServiceDefaultRoles"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "ExternalServiceDefaultRoles/ScopedServiceDefaultRoles", "ExternalServiceDefaultRoles:ScopedServiceDefaultRoles"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserDelegation/DelegationEmailExpirationHours", "UserDelegation:DelegationEmailExpirationHours"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserDelegation/DelegationEmailTokenEncryptionKey", "UserDelegation:DelegationEmailTokenEncryptionKey"));
      
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "UserDelegation/DelegationExcludeRoles", "UserDelegation:DelegationExcludeRoles"));

      // #Auto validation
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidation/Enable", "OrgAutoValidation:Enable"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OrgAutoValidation/CCSAdminEmailIds", "OrgAutoValidation:CCSAdminEmailIds"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OrgAutoValidation/BuyerSuccessAdminRoles", "OrgAutoValidation:BuyerSuccessAdminRoles"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OrgAutoValidation/BothSuccessAdminRoles", "OrgAutoValidation:BothSuccessAdminRoles"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "LookUpApiSettings/LookUpApiKey", "LookUpApiSettings:LookUpApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "LookUpApiSettings/LookUpApiUrl", "LookUpApiSettings:LookUpApiUrl"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidationEmail/DeclineRightToBuyStatusEmailTemplateId", "OrgAutoValidationEmail:DeclineRightToBuyStatusEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidationEmail/ApproveRightToBuyStatusEmailTemplateId", "OrgAutoValidationEmail:ApproveRightToBuyStatusEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidationEmail/RemoveRightToBuyStatusEmailTemplateId", "OrgAutoValidationEmail:RemoveRightToBuyStatusEmailTemplateId"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidationEmail/OrgPendingVerificationEmailTemplateId", "OrgAutoValidationEmail:OrgPendingVerificationEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OrgAutoValidationEmail/OrgBuyerStatusChangeUpdateToAllAdmins", "OrgAutoValidationEmail:OrgBuyerStatusChangeUpdateToAllAdmins"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserRoleApproval/Enable", "UserRoleApproval:Enable"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserRoleApproval/RoleApprovalTokenEncryptionKey", "UserRoleApproval:RoleApprovalTokenEncryptionKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserRoleApproval/UserRoleApprovalEmailTemplateId", "UserRoleApproval:UserRoleApprovalEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserRoleApproval/UserRoleApprovedEmailTemplateId", "UserRoleApproval:UserRoleApprovedEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "UserRoleApproval/UserRoleRejectedEmailTemplateId", "UserRoleApproval:UserRoleRejectedEmailTemplateId"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ServiceRoleGroupSettings/Enable", "ServiceRoleGroupSettings:Enable"));
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
