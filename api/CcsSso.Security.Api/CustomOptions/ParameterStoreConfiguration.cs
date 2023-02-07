using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Logs;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.CustomOptions
{
  public class ParameterStoreConfigurationProvider : ConfigurationProvider
  {
    private string path = "/conclave-sso/security/";
    private IAwsParameterStoreService _awsParameterStoreService;

    public ParameterStoreConfigurationProvider()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
    }

    public override void Load()
    {
      LoadAsync().Wait();
      if (Data.ContainsKey("Serilog"))
      {
        LogConfigurationManager.ConfigureLogs(Data["Serilog"]);
      }
    }

    public async Task LoadAsync()
    {
      await GetSecrets();
    }

    public async Task GetSecrets()
    {
      var parameters = await _awsParameterStoreService.GetParameters(path);
      var configurations = new List<KeyValuePair<string, string>>();

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "EnableAdditionalLogs", "EnableAdditionalLogs"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "CustomDomain", "CustomDomain"));

      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "CorsDomains", "CorsDomains"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "AllowedDomains", "AllowedDomains"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "PasswordPolicy/LowerAndUpperCaseWithDigits", "PasswordPolicy:LowerAndUpperCaseWithDigits"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "PasswordPolicy/RequiredLength", "PasswordPolicy:RequiredLength"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "PasswordPolicy/RequiredUniqueChars", "PasswordPolicy:RequiredUniqueChars"));

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

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SessionConfig/SessionTimeoutInMinutes", "SessionConfig:SessionTimeoutInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SessionConfig/StateExpirationInMinutes", "SessionConfig:StateExpirationInMinutes"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "SecurityApiKeySettings/SecurityApiKey", "SecurityApiKeySettings:SecurityApiKey"));

      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "SecurityApiKeySettings/ApiKeyValidationExcludedRoutes", "SecurityApiKeySettings:ApiKeyValidationExcludedRoutes"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "SecurityApiKeySettings/BearerTokenValidationIncludedRoutes", "SecurityApiKeySettings:BearerTokenValidationIncludedRoutes"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/Issuer", "JwtTokenConfig:Issuer"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/RsaPrivateKey", "JwtTokenConfig:RsaPrivateKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/RsaPublicKey", "JwtTokenConfig:RsaPublicKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/IDTokenExpirationTimeInMinutes", "JwtTokenConfig:IDTokenExpirationTimeInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/LogoutTokenExpireTimeInMinutes", "JwtTokenConfig:LogoutTokenExpireTimeInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/JwksUrl", "JwtTokenConfig:JwksUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "JwtTokenConfig/IdamClienId", "JwtTokenConfig:IdamClienId"));

      // Keep the trailing "/" for all the urls. Ex: "https://abc.com/user-profiles/"
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApi/ApiKey", "WrapperApi:ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApi/UserServiceUrl", "WrapperApi:UserServiceUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "WrapperApi/ConfigurationServiceUrl", "WrapperApi:ConfigurationServiceUrl"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "RollBarLogger/Token", "RollBarLogger:Token"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "RollBarLogger/Environment", "RollBarLogger:Environment"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Serilog", "Serilog"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "MockProvider/LoginUrl", "MockProvider:LoginUrl"));

      var securityDbName = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityDbName");
      var securityDbConnection = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityDbConnection");

      if(!string.IsNullOrEmpty(securityDbName))
      {
        var dynamicSecurityDbConnection = UtilityHelper.GetDatbaseConnectionString(securityDbName, securityDbConnection);
        Data.Add("SecurityDbConnection", dynamicSecurityDbConnection);
      }
      else
      {
        Data.Add("SecurityDbConnection", securityDbConnection);
      }

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Crypto/CookieEncryptionKey", "Crypto:CookieEncryptionKey"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "MfaSettings/TicketExpirationInMinutes", "MfaSettings:TicketExpirationInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "MfaSettings/MfaResetRedirectUri", "MfaSettings:MfaResetRedirectUri"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "MfaSettings/MFAResetPersistentTicketListExpirationInDays", "MfaSettings:MFAResetPersistentTicketListExpirationInDays"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/Issuer", "OpenIdConfigurationSettings:Issuer"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/AuthorizationEndpoint", "OpenIdConfigurationSettings:AuthorizationEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/TokenEndpoint", "OpenIdConfigurationSettings:TokenEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/DeviceAuthorizationEndpoint", "OpenIdConfigurationSettings:DeviceAuthorizationEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/UserinfoEndpoint", "OpenIdConfigurationSettings:UserinfoEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/MfaChallengeEndpoint", "OpenIdConfigurationSettings:MfaChallengeEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/JwksUri", "OpenIdConfigurationSettings:JwksUri"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/RegistrationEndpoint", "OpenIdConfigurationSettings:RegistrationEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/RevocationEndpoint", "OpenIdConfigurationSettings:RevocationEndpoint"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "OpenIdConfigurationSettings/RequestUriParameterSupported", "OpenIdConfigurationSettings:RequestUriParameterSupported"));

      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ScopesSupported", "OpenIdConfigurationSettings:ScopesSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ResponseTypesSupported", "OpenIdConfigurationSettings:ResponseTypesSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/CodeChallengeMethodsSupported", "OpenIdConfigurationSettings:CodeChallengeMethodsSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ResponseModesSupported", "OpenIdConfigurationSettings:ResponseModesSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/SubjectTypesSupported", "OpenIdConfigurationSettings:SubjectTypesSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/IdTokenSigningAlgValuesSupported", "OpenIdConfigurationSettings:IdTokenSigningAlgValuesSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/TokenEndpointAuthMethodsSupported", "OpenIdConfigurationSettings:TokenEndpointAuthMethodsSupported"));
      configurations.AddRange(_awsParameterStoreService.GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ClaimsSupported", "OpenIdConfigurationSettings:ClaimsSupported"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "IsApiGatewayEnabled", "IsApiGatewayEnabled"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/ClientId", "Auth0:ClientId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/Secret", "Auth0:Secret"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/Domain", "Auth0:Domain"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/DBConnectionName", "Auth0:DBConnectionName"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/ManagementApiBaseUrl", "Auth0:ManagementApiBaseUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/ManagementApiIdentifier", "Auth0:ManagementApiIdentifier"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/DefaultAudience", "Auth0:DefaultAudience"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/UserStore", "Auth0:UserStore"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Auth0/DefaultDBConnectionId", "Auth0:DefaultDBConnectionId"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "AWSCognito/Region", "AWSCognito:Region"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "AWSCognito/PoolId", "AWSCognito:PoolId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "AWSCognito/AppClientId", "AWSCognito:AppClientId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "AWSCognito/AccessKeyId", "AWSCognito:AccessKeyId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "AWSCognito/AccessSecretKey", "AWSCognito:AccessSecretKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "AWSCognito/AWSCognitoURL", "AWSCognito:AWSCognitoURL"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "IdentityProvider", "IdentityProvider"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/ApiKey", "Email:ApiKey"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserActivationEmailTemplateId", "Email:UserActivationEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/ResetPasswordEmailTemplateId", "Email:ResetPasswordEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/NominateEmailTemplateId", "Email:NominateEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/MfaResetEmailTemplateId", "Email:MfaResetEmailTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/UserActivationLinkTTLInMinutes", "Email:UserActivationLinkTTLInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/ChangePasswordNotificationTemplateId", "Email:ChangePasswordNotificationTemplateId"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/ResetPasswordLinkTTLInMinutes", "Email:ResetPasswordLinkTTLInMinutes"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "Email/SendNotificationsEnabled", "Email:SendNotificationsEnabled"));

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ResetPasswordSettings/MaxAllowedAttempts", "ResetPasswordSettings:MaxAllowedAttempts"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "ResetPasswordSettings/MaxAllowedAttemptsThresholdInMinutes", "ResetPasswordSettings:MaxAllowedAttemptsThresholdInMinutes"));

      var queueDataName = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataName"); // Data Queue

      if (!string.IsNullOrEmpty(queueDataName))
      {
        var queueInfo = UtilityHelper.GetSqsSetting(queueDataName);
        Data.Add("QueueInfo:DataQueueAccessKeyId", queueInfo.credentials.aws_access_key_id);
        Data.Add("QueueInfo:DataQueueAccessSecretKey", queueInfo.credentials.aws_secret_access_key);
        Data.Add("QueueInfo:DataQueueUrl", queueInfo.credentials.primary_queue_url);
      }
      else
      {
        Data.Add("QueueInfo:DataQueueAccessKeyId", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueAccessKeyId"));
        Data.Add("QueueInfo:DataQueueAccessSecretKey", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueAccessSecretKey"));
        Data.Add("QueueInfo:DataQueueUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueUrl"));
      }

      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/ServiceUrl", "QueueInfo:ServiceUrl"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/EnableDataQueue", "QueueInfo:EnableDataQueue"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/DataQueueRecieveMessagesMaxCount", "QueueInfo:DataQueueRecieveMessagesMaxCount"));
      configurations.Add(_awsParameterStoreService.GetParameter(parameters, path + "QueueInfo/DataQueueRecieveWaitTimeInSeconds", "QueueInfo:DataQueueRecieveWaitTimeInSeconds"));

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

