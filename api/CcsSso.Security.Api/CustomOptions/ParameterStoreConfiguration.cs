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

      Data.Add("EnableAdditionalLogs", _awsParameterStoreService.FindParameterByName(parameters, path + "EnableAdditionalLogs"));
      Data.Add("CustomDomain", _awsParameterStoreService.FindParameterByName(parameters, path + "CustomDomain"));

      GetParameterFromCommaSeparated(parameters, path + "CorsDomains", "CorsDomains");
      GetParameterFromCommaSeparated(parameters, path + "AllowedDomains", "AllowedDomains");

      Data.Add("PasswordPolicy:LowerAndUpperCaseWithDigits", _awsParameterStoreService.FindParameterByName(parameters, path + "PasswordPolicy/LowerAndUpperCaseWithDigits"));
      Data.Add("PasswordPolicy:RequiredLength", _awsParameterStoreService.FindParameterByName(parameters, path + "PasswordPolicy/RequiredLength"));
      Data.Add("PasswordPolicy:RequiredUniqueChars", _awsParameterStoreService.FindParameterByName(parameters, path + "PasswordPolicy/RequiredUniqueChars"));

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

      Data.Add("SessionConfig:SessionTimeoutInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "SessionConfig/SessionTimeoutInMinutes"));
      Data.Add("SessionConfig:StateExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "SessionConfig/StateExpirationInMinutes"));

      Data.Add("SecurityApiKeySettings:SecurityApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiKeySettings/SecurityApiKey"));

      GetParameterFromCommaSeparated(parameters, path + "SecurityApiKeySettings/ApiKeyValidationExcludedRoutes", "SecurityApiKeySettings:ApiKeyValidationExcludedRoutes");
      GetParameterFromCommaSeparated(parameters, path + "SecurityApiKeySettings/BearerTokenValidationIncludedRoutes", "SecurityApiKeySettings:BearerTokenValidationIncludedRoutes");

      Data.Add("JwtTokenConfig:Issuer", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/Issuer"));
      Data.Add("JwtTokenConfig:RsaPrivateKey", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/RsaPrivateKey"));
      Data.Add("JwtTokenConfig:RsaPublicKey", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/RsaPublicKey"));
      Data.Add("JwtTokenConfig:IDTokenExpirationTimeInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/IDTokenExpirationTimeInMinutes"));
      Data.Add("JwtTokenConfig:LogoutTokenExpireTimeInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/LogoutTokenExpireTimeInMinutes"));
      Data.Add("JwtTokenConfig:JwksUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/JwksUrl"));
      Data.Add("JwtTokenConfig:IdamClienId", _awsParameterStoreService.FindParameterByName(parameters, path + "JwtTokenConfig/IdamClienId"));

      // Keep the trailing "/" for all the urls. Ex: "https://abc.com/user-profiles/"
      Data.Add("WrapperApi:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApi/ApiKey"));
      Data.Add("WrapperApi:UserServiceUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApi/UserServiceUrl"));
      Data.Add("WrapperApi:ConfigurationServiceUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApi/ConfigurationServiceUrl"));

      Data.Add("RollBarLogger:Token", _awsParameterStoreService.FindParameterByName(parameters, path + "RollBarLogger/Token"));
      Data.Add("RollBarLogger:Environment", _awsParameterStoreService.FindParameterByName(parameters, path + "RollBarLogger/Environment"));

      Data.Add("Serilog", _awsParameterStoreService.FindParameterByName(parameters, path + "Serilog"));

      Data.Add("MockProvider:LoginUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "MockProvider/LoginUrl"));

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

      Data.Add("Crypto:CookieEncryptionKey", _awsParameterStoreService.FindParameterByName(parameters, path + "Crypto/CookieEncryptionKey"));

      Data.Add("MfaSettings:TicketExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "MfaSettings/TicketExpirationInMinutes"));
      Data.Add("MfaSettings:MfaResetRedirectUri", _awsParameterStoreService.FindParameterByName(parameters, path + "MfaSettings/MfaResetRedirectUri"));
      Data.Add("MfaSettings:MFAResetPersistentTicketListExpirationInDays", _awsParameterStoreService.FindParameterByName(parameters, path + "MfaSettings/MFAResetPersistentTicketListExpirationInDays"));

      Data.Add("OpenIdConfigurationSettings:Issuer", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/Issuer"));
      Data.Add("OpenIdConfigurationSettings:AuthorizationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/AuthorizationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:TokenEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/TokenEndpoint"));
      Data.Add("OpenIdConfigurationSettings:DeviceAuthorizationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/DeviceAuthorizationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:UserinfoEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/UserinfoEndpoint"));
      Data.Add("OpenIdConfigurationSettings:MfaChallengeEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/MfaChallengeEndpoint"));
      Data.Add("OpenIdConfigurationSettings:JwksUri", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/JwksUri"));
      Data.Add("OpenIdConfigurationSettings:RegistrationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/RegistrationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:RevocationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/RevocationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:RequestUriParameterSupported", _awsParameterStoreService.FindParameterByName(parameters, path + "OpenIdConfigurationSettings/RequestUriParameterSupported"));

      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ScopesSupported", "OpenIdConfigurationSettings:ScopesSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ResponseTypesSupported", "OpenIdConfigurationSettings:ResponseTypesSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/CodeChallengeMethodsSupported", "OpenIdConfigurationSettings:CodeChallengeMethodsSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ResponseModesSupported", "OpenIdConfigurationSettings:ResponseModesSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/SubjectTypesSupported", "OpenIdConfigurationSettings:SubjectTypesSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/IdTokenSigningAlgValuesSupported", "OpenIdConfigurationSettings:IdTokenSigningAlgValuesSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/TokenEndpointAuthMethodsSupported", "OpenIdConfigurationSettings:TokenEndpointAuthMethodsSupported");
      GetParameterFromCommaSeparated(parameters, path + "OpenIdConfigurationSettings/ClaimsSupported", "OpenIdConfigurationSettings:ClaimsSupported");

      Data.Add("IsApiGatewayEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "IsApiGatewayEnabled"));

      Data.Add("Auth0:ClientId", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/ClientId"));
      Data.Add("Auth0:Secret", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/Secret"));
      Data.Add("Auth0:Domain", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/Domain"));
      Data.Add("Auth0:DBConnectionName", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/DBConnectionName"));
      Data.Add("Auth0:ManagementApiBaseUrl", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/ManagementApiBaseUrl"));
      Data.Add("Auth0:ManagementApiIdentifier", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/ManagementApiIdentifier"));
      Data.Add("Auth0:DefaultAudience", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/DefaultAudience"));
      Data.Add("Auth0:UserStore", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/UserStore"));
      Data.Add("Auth0:DefaultDBConnectionId", _awsParameterStoreService.FindParameterByName(parameters, path + "Auth0/DefaultDBConnectionId"));

      Data.Add("AWSCognito:Region", _awsParameterStoreService.FindParameterByName(parameters, path + "AWSCognito/Region"));
      Data.Add("AWSCognito:PoolId", _awsParameterStoreService.FindParameterByName(parameters, path + "AWSCognito/PoolId"));
      Data.Add("AWSCognito:AppClientId", _awsParameterStoreService.FindParameterByName(parameters, path + "AWSCognito/AppClientId"));
      Data.Add("AWSCognito:AccessKeyId", _awsParameterStoreService.FindParameterByName(parameters, path + "AWSCognito/AccessKeyId"));
      Data.Add("AWSCognito:AccessSecretKey", _awsParameterStoreService.FindParameterByName(parameters, path + "AWSCognito/AccessSecretKey"));
      Data.Add("AWSCognito:AWSCognitoURL", _awsParameterStoreService.FindParameterByName(parameters, path + "AWSCognito/AWSCognitoURL"));

      Data.Add("IdentityProvider", _awsParameterStoreService.FindParameterByName(parameters, path + "IdentityProvider"));

      Data.Add("Email:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ApiKey"));
      Data.Add("Email:UserActivationEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserActivationEmailTemplateId"));
      Data.Add("Email:ResetPasswordEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ResetPasswordEmailTemplateId"));
      Data.Add("Email:NominateEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/NominateEmailTemplateId"));
      Data.Add("Email:MfaResetEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/MfaResetEmailTemplateId"));
      Data.Add("Email:UserActivationLinkTTLInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserActivationLinkTTLInMinutes"));
      Data.Add("Email:ChangePasswordNotificationTemplateId", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ChangePasswordNotificationTemplateId"));
      Data.Add("Email:ResetPasswordLinkTTLInMinutes", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ResetPasswordLinkTTLInMinutes"));
      Data.Add("Email:SendNotificationsEnabled", _awsParameterStoreService.FindParameterByName(parameters, path + "Email/SendNotificationsEnabled"));

      foreach (var item in Data)
      {
        Console.WriteLine(item.Key + ":" + item.Value);
      }
    }

    private void GetParameterFromCommaSeparated(List<Parameter> parameters, string name, string key)
    {
      string value = _awsParameterStoreService.FindParameterByName(parameters, name);
      if (value != null)
      {
        List<string> items = value.Split(',').ToList();
        int index = 0;
        foreach (var item in items)
        {
          Data.Add($"{key}:{index++}", item);
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

