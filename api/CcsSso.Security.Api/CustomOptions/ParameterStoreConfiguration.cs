using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Logs;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
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

      Data.Add("EnableAdditionalLogs", _awsParameterStoreService.FindParameterByName(parameters, "EnableAdditionalLogs"));
      Data.Add("CustomDomain", _awsParameterStoreService.FindParameterByName(parameters, "CustomDomain"));

      GetParameterFromCommaSeparated(parameters, "CorsDomains", "CorsDomains");
      GetParameterFromCommaSeparated(parameters, "AllowedDomains", "AllowedDomains");

      Data.Add("PasswordPolicy:LowerAndUpperCaseWithDigits", _awsParameterStoreService.FindParameterByName(parameters, "PasswordPolicy/LowerAndUpperCaseWithDigits"));
      Data.Add("PasswordPolicy:RequiredLength", _awsParameterStoreService.FindParameterByName(parameters, "PasswordPolicy/RequiredLength"));
      Data.Add("PasswordPolicy:RequiredUniqueChars", _awsParameterStoreService.FindParameterByName(parameters, "PasswordPolicy/RequiredUniqueChars"));

      var redisCacheName = _awsParameterStoreService.FindParameterByName(parameters, "RedisCacheSettings/Name");
      var redisCacheConnectionString = _awsParameterStoreService.FindParameterByName(parameters, "RedisCacheSettings/ConnectionString");

      if (!string.IsNullOrEmpty(redisCacheName))
      {
        var dynamicRedisCacheConnectionString = UtilityHelper.GetRedisCacheConnectionString(redisCacheName, redisCacheConnectionString);
        Data.Add("RedisCacheSettings:ConnectionString", dynamicRedisCacheConnectionString);
      }
      else
      {
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheConnectionString);
      }

      Data.Add("RedisCacheSettings:IsEnabled", _awsParameterStoreService.FindParameterByName(parameters, "RedisCacheSettings/IsEnabled"));

      Data.Add("SessionConfig:SessionTimeoutInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "SessionConfig/SessionTimeoutInMinutes"));
      Data.Add("SessionConfig:StateExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "SessionConfig/StateExpirationInMinutes"));

      Data.Add("SecurityApiKeySettings:SecurityApiKey", _awsParameterStoreService.FindParameterByName(parameters, "SecurityApiKeySettings/SecurityApiKey"));

      GetParameterFromCommaSeparated(parameters, "SecurityApiKeySettings/ApiKeyValidationExcludedRoutes", "SecurityApiKeySettings:ApiKeyValidationExcludedRoutes");
      GetParameterFromCommaSeparated(parameters, "SecurityApiKeySettings/BearerTokenValidationIncludedRoutes", "SecurityApiKeySettings:BearerTokenValidationIncludedRoutes");

      Data.Add("JwtTokenConfig:Issuer", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/Issuer"));
      Data.Add("JwtTokenConfig:RsaPrivateKey", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/RsaPrivateKey"));
      Data.Add("JwtTokenConfig:RsaPublicKey", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/RsaPublicKey"));
      Data.Add("JwtTokenConfig:IDTokenExpirationTimeInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/IDTokenExpirationTimeInMinutes"));
      Data.Add("JwtTokenConfig:LogoutTokenExpireTimeInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/LogoutTokenExpireTimeInMinutes"));
      Data.Add("JwtTokenConfig:JwksUrl", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/JwksUrl"));
      Data.Add("JwtTokenConfig:IdamClienId", _awsParameterStoreService.FindParameterByName(parameters, "JwtTokenConfig/IdamClienId"));

      // Keep the trailing "/" for all the urls. Ex: "https://abc.com/user-profiles/"
      Data.Add("WrapperApi:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, "WrapperApi/ApiKey"));
      Data.Add("WrapperApi:UserServiceUrl", _awsParameterStoreService.FindParameterByName(parameters, "WrapperApi/UserServiceUrl"));
      Data.Add("WrapperApi:ConfigurationServiceUrl", _awsParameterStoreService.FindParameterByName(parameters, "WrapperApi/ConfigurationServiceUrl"));

      Data.Add("RollBarLogger:Token", _awsParameterStoreService.FindParameterByName(parameters, "RollBarLogger/Token"));
      Data.Add("RollBarLogger:Environment", _awsParameterStoreService.FindParameterByName(parameters, "RollBarLogger/Environment"));

      Data.Add("Serilog", _awsParameterStoreService.FindParameterByName(parameters, "Serilog"));

      Data.Add("MockProvider:LoginUrl", _awsParameterStoreService.FindParameterByName(parameters, "MockProvider/LoginUrl"));

      var securityDbName = _awsParameterStoreService.FindParameterByName(parameters, "SecurityDbName");
      var securityDbConnection = _awsParameterStoreService.FindParameterByName(parameters, "SecurityDbConnection");

      if(!string.IsNullOrEmpty(securityDbName))
      {
        var dynamicSecurityDbConnection = UtilityHelper.GetDatbaseConnectionString(securityDbName, securityDbConnection);
        Data.Add("SecurityDbConnection", dynamicSecurityDbConnection);
      }
      else
      {
        Data.Add("SecurityDbConnection", securityDbConnection);
      }

      Data.Add("Crypto:CookieEncryptionKey", _awsParameterStoreService.FindParameterByName(parameters, "Crypto/CookieEncryptionKey"));

      Data.Add("MfaSettings:TicketExpirationInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "MfaSettings/TicketExpirationInMinutes"));
      Data.Add("MfaSettings:MfaResetRedirectUri", _awsParameterStoreService.FindParameterByName(parameters, "MfaSettings/MfaResetRedirectUri"));
      Data.Add("MfaSettings:MFAResetPersistentTicketListExpirationInDays", _awsParameterStoreService.FindParameterByName(parameters, "MfaSettings/MFAResetPersistentTicketListExpirationInDays"));

      Data.Add("OpenIdConfigurationSettings:Issuer", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/Issuer"));
      Data.Add("OpenIdConfigurationSettings:AuthorizationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/AuthorizationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:TokenEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/TokenEndpoint"));
      Data.Add("OpenIdConfigurationSettings:DeviceAuthorizationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/DeviceAuthorizationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:UserinfoEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/UserinfoEndpoint"));
      Data.Add("OpenIdConfigurationSettings:MfaChallengeEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/MfaChallengeEndpoint"));
      Data.Add("OpenIdConfigurationSettings:JwksUri", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/JwksUri"));
      Data.Add("OpenIdConfigurationSettings:RegistrationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/RegistrationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:RevocationEndpoint", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/RevocationEndpoint"));
      Data.Add("OpenIdConfigurationSettings:RequestUriParameterSupported", _awsParameterStoreService.FindParameterByName(parameters, "OpenIdConfigurationSettings/RequestUriParameterSupported"));

      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/ScopesSupported", "OpenIdConfigurationSettings:ScopesSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/ResponseTypesSupported", "OpenIdConfigurationSettings:ResponseTypesSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/CodeChallengeMethodsSupported", "OpenIdConfigurationSettings:CodeChallengeMethodsSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/ResponseModesSupported", "OpenIdConfigurationSettings:ResponseModesSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/SubjectTypesSupported", "OpenIdConfigurationSettings:SubjectTypesSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/IdTokenSigningAlgValuesSupported", "OpenIdConfigurationSettings:IdTokenSigningAlgValuesSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/TokenEndpointAuthMethodsSupported", "OpenIdConfigurationSettings:TokenEndpointAuthMethodsSupported");
      GetParameterFromCommaSeparated(parameters, "OpenIdConfigurationSettings/ClaimsSupported", "OpenIdConfigurationSettings:ClaimsSupported");

      Data.Add("IsApiGatewayEnabled", _awsParameterStoreService.FindParameterByName(parameters, "IsApiGatewayEnabled"));

      Data.Add("Auth0:ClientId", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/ClientId"));
      Data.Add("Auth0:Secret", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/Secret"));
      Data.Add("Auth0:Domain", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/Domain"));
      Data.Add("Auth0:DBConnectionName", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/DBConnectionName"));
      Data.Add("Auth0:ManagementApiBaseUrl", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/ManagementApiBaseUrl"));
      Data.Add("Auth0:ManagementApiIdentifier", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/ManagementApiIdentifier"));
      Data.Add("Auth0:DefaultAudience", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/DefaultAudience"));
      Data.Add("Auth0:UserStore", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/UserStore"));
      Data.Add("Auth0:DefaultDBConnectionId", _awsParameterStoreService.FindParameterByName(parameters, "Auth0/DefaultDBConnectionId"));

      Data.Add("AWSCognito:Region", _awsParameterStoreService.FindParameterByName(parameters, "AWSCognito/Region"));
      Data.Add("AWSCognito:PoolId", _awsParameterStoreService.FindParameterByName(parameters, "AWSCognito/PoolId"));
      Data.Add("AWSCognito:AppClientId", _awsParameterStoreService.FindParameterByName(parameters, "AWSCognito/AppClientId"));
      Data.Add("AWSCognito:AccessKeyId", _awsParameterStoreService.FindParameterByName(parameters, "AWSCognito/AccessKeyId"));
      Data.Add("AWSCognito:AccessSecretKey", _awsParameterStoreService.FindParameterByName(parameters, "AWSCognito/AccessSecretKey"));
      Data.Add("AWSCognito:AWSCognitoURL", _awsParameterStoreService.FindParameterByName(parameters, "AWSCognito/AWSCognitoURL"));

      Data.Add("IdentityProvider", _awsParameterStoreService.FindParameterByName(parameters, "IdentityProvider"));

      Data.Add("Email:ApiKey", _awsParameterStoreService.FindParameterByName(parameters, "Email/ApiKey"));
      Data.Add("Email:UserActivationEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, "Email/UserActivationEmailTemplateId"));
      Data.Add("Email:ResetPasswordEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, "Email/ResetPasswordEmailTemplateId"));
      Data.Add("Email:NominateEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, "Email/NominateEmailTemplateId"));
      Data.Add("Email:MfaResetEmailTemplateId", _awsParameterStoreService.FindParameterByName(parameters, "Email/MfaResetEmailTemplateId"));
      Data.Add("Email:UserActivationLinkTTLInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "Email/UserActivationLinkTTLInMinutes"));
      Data.Add("Email:ChangePasswordNotificationTemplateId", _awsParameterStoreService.FindParameterByName(parameters, "Email/ChangePasswordNotificationTemplateId"));
      Data.Add("Email:ResetPasswordLinkTTLInMinutes", _awsParameterStoreService.FindParameterByName(parameters, "Email/ResetPasswordLinkTTLInMinutes"));
      Data.Add("Email:SendNotificationsEnabled", _awsParameterStoreService.FindParameterByName(parameters, "Email/SendNotificationsEnabled"));

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

