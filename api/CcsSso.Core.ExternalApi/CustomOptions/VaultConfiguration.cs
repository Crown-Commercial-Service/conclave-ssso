using System;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;
using Microsoft.Extensions.Configuration;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using CcsSso.Shared.Domain;
using CcsSso.Domain.Dtos;

namespace CcsSso.ExternalApi.Api.CustomOptions
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
      Console.WriteLine(env);
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
      var _secrets = await _client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/wrapper", mountPathValue);
      var _dbConnection = _secrets.Data["DbConnection"].ToString();
      var _key = _secrets.Data["ApiKey"].ToString();
      var _isApiGatewayEnabled = _secrets.Data["IsApiGatewayEnabled"].ToString();

      if (_secrets.Data.ContainsKey("CorsDomains"))
      {
        var corsList = JsonConvert.DeserializeObject<List<string>>(_secrets.Data["CorsDomains"].ToString());
        int index = 0;
        foreach (var cors in corsList)
        {
          Data.Add($"CorsDomains:{index++}", cors);
        }
      }
      var _conclaveLoginUrl = _secrets.Data["ConclaveLoginUrl"].ToString();
      var _inMemoryCacheExpirationInMinutes = _secrets.Data["InMemoryCacheExpirationInMinutes"].ToString();
      var _dashboardServiceClientId = _secrets.Data["DashboardServiceClientId"].ToString();

      Data.Add("DbConnection", _dbConnection);
      Data.Add("ApiKey", _key);
      Data.Add("IsApiGatewayEnabled", _isApiGatewayEnabled);
      Data.Add("EnableAdditionalLogs", _secrets.Data["EnableAdditionalLogs"].ToString());
      Data.Add("EnableUserAccessTokenFix", _secrets.Data["EnableUserAccessTokenFix"].ToString());
      Data.Add("ConclaveLoginUrl", _conclaveLoginUrl);
      Data.Add("InMemoryCacheExpirationInMinutes", _inMemoryCacheExpirationInMinutes);
      Data.Add("DashboardServiceClientId", _dashboardServiceClientId);

      if (_secrets.Data.ContainsKey("JwtTokenValidationInfo"))
      {
        var jwtTokenValidationInfoVault = JsonConvert.DeserializeObject<JwtTokenValidationInfoVault>(_secrets.Data["JwtTokenValidationInfo"].ToString());
        Data.Add("JwtTokenValidationInfo:IdamClienId", jwtTokenValidationInfoVault.IdamClienId);
        Data.Add("JwtTokenValidationInfo:Issuer", jwtTokenValidationInfoVault.Issuer);
        Data.Add("JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl", jwtTokenValidationInfoVault.ApiGatewayEnabledJwksUrl);
        Data.Add("JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl", jwtTokenValidationInfoVault.ApiGatewayDisabledJwksUrl);
        Data.Add("Cii:Client_ID", jwtTokenValidationInfoVault.IdamClienId);
      }

      if (_secrets.Data.ContainsKey("SecurityApiSettings"))
      {
        var securityApiKeySettings = JsonConvert.DeserializeObject<SecurityApiSettingsVault>(_secrets.Data["SecurityApiSettings"].ToString());
        Data.Add("SecurityApiSettings:ApiKey", securityApiKeySettings.ApiKey);
        Data.Add("SecurityApiSettings:Url", securityApiKeySettings.Url);
      }

      if (_secrets.Data.ContainsKey("Email"))
      {
        var emailsettings = JsonConvert.DeserializeObject<Email>(_secrets.Data["Email"].ToString());
        Data.Add("Email:ApiKey", emailsettings.ApiKey);
        Data.Add("Email:UserWelcomeEmailTemplateId", emailsettings.UserWelcomeEmailTemplateId);
        Data.Add("Email:OrgProfileUpdateNotificationTemplateId", emailsettings.OrgProfileUpdateNotificationTemplateId);
        Data.Add("Email:UserContactUpdateNotificationTemplateId", emailsettings.UserContactUpdateNotificationTemplateId);
        Data.Add("Email:UserProfileUpdateNotificationTemplateId", emailsettings.UserProfileUpdateNotificationTemplateId);
        Data.Add("Email:UserPermissionUpdateNotificationTemplateId", emailsettings.UserPermissionUpdateNotificationTemplateId);

        Data.Add("Email:UserUpdateEmailOnlyFederatedIdpTemplateId", emailsettings.UserUpdateEmailOnlyFederatedIdpTemplateId);
        Data.Add("Email:UserUpdateEmailBothIdpTemplateId", emailsettings.UserUpdateEmailBothIdpTemplateId);
        Data.Add("Email:UserUpdateEmailOnlyUserIdPwdTemplateId", emailsettings.UserUpdateEmailOnlyUserIdPwdTemplateId);

        Data.Add("Email:UserConfirmEmailOnlyFederatedIdpTemplateId", emailsettings.UserConfirmEmailOnlyFederatedIdpTemplateId);
        Data.Add("Email:UserConfirmEmailBothIdpTemplateId", emailsettings.UserConfirmEmailBothIdpTemplateId);
        Data.Add("Email:UserConfirmEmailOnlyUserIdPwdTemplateId", emailsettings.UserConfirmEmailOnlyUserIdPwdTemplateId);
        Data.Add("Email:UserRegistrationEmailUserIdPwdTemplateId", emailsettings.UserRegistrationEmailUserIdPwdTemplateId);
        // #Delegated
        Data.Add("Email:UserDelegatedAccessEmailTemplateId", emailsettings.UserDelegatedAccessEmailTemplateId);

        Data.Add("Email:SendNotificationsEnabled", emailsettings.SendNotificationsEnabled);
      }

      if (_secrets.Data.ContainsKey("Cii"))
      {
        var _cii = JsonConvert.DeserializeObject<Cii>(_secrets.Data["Cii"].ToString());
        Data.Add("Cii:Url", _cii.url);
        Data.Add("Cii:Token", _cii.token);
        Data.Add("Cii:Delete_Token", _cii.token_delete);
      }

      if (_secrets.Data.ContainsKey("QueueInfo"))
      {
        var queueInfo = JsonConvert.DeserializeObject<QueueInfoVault>(_secrets.Data["QueueInfo"].ToString());
        Data.Add("QueueInfo:AccessKeyId", queueInfo.AccessKeyId);
        Data.Add("QueueInfo:AccessSecretKey", queueInfo.AccessSecretKey);
        Data.Add("QueueInfo:ServiceUrl", queueInfo.ServiceUrl);
        Data.Add("QueueInfo:RecieveMessagesMaxCount", queueInfo.RecieveMessagesMaxCount);
        Data.Add("QueueInfo:RecieveWaitTimeInSeconds", queueInfo.RecieveWaitTimeInSeconds);
        Data.Add("QueueInfo:EnableAdaptorNotifications", queueInfo.EnableAdaptorNotifications);
        Data.Add("QueueInfo:AdaptorNotificationQueueUrl", queueInfo.AdaptorNotificationQueueUrl);
      }

      if (_secrets.Data.ContainsKey("RedisCacheSettings"))
      {
        var redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(_secrets.Data["RedisCacheSettings"].ToString());
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheSettingsVault.ConnectionString);
        Data.Add("RedisCacheSettings:IsEnabled", redisCacheSettingsVault.IsEnabled);
        Data.Add("RedisCacheSettings:CacheExpirationInMinutes", redisCacheSettingsVault.CacheExpirationInMinutes);
      }

      if (_secrets.Data.ContainsKey("ExternalServiceDefaultRoles"))
      {
        var externalServiceDefaultRolesSettings = JsonConvert.DeserializeObject<ExternalServiceDefaultRoles>(_secrets.Data["ExternalServiceDefaultRoles"].ToString());
        int index = 0;
        foreach (var globalRole in externalServiceDefaultRolesSettings.GlobalServiceDefaultRoles)
        {
          Data.Add($"ExternalServiceDefaultRoles:GlobalServiceDefaultRoles:{index++}", globalRole);
        }
        index = 0;
        foreach (var scopedRole in externalServiceDefaultRolesSettings.ScopedServiceDefaultRoles)
        {
          Data.Add($"ExternalServiceDefaultRoles:ScopedServiceDefaultRoles:{index++}", scopedRole);
        }
      }
      // #Delegated
      if (_secrets.Data.ContainsKey("UserDelegation"))
      {
        var userDelegationInfo = JsonConvert.DeserializeObject<UserDelegation>(_secrets.Data["UserDelegation"].ToString());
        Data.Add("UserDelegation:DelegationEmailExpirationHours", userDelegationInfo.DelegationEmailExpirationHours.ToString());
        Data.Add("UserDelegation:DelegationEmailTokenEncryptionKey", userDelegationInfo.DelegationEmailTokenEncryptionKey);
        
        int index = 0;
        foreach (var excludeRole in userDelegationInfo.DelegationExcludeRoles)
        {
          Data.Add($"UserDelegation:DelegationExcludeRoles:{index++}", excludeRole);
        }
      }

      // #Auto validation
      if (_secrets.Data.ContainsKey("OrgAutoValidation"))
      {
        var orgAutoValidation = JsonConvert.DeserializeObject<OrgAutoValidation>(_secrets.Data["OrgAutoValidation"].ToString());
        Data.Add("OrgAutoValidation:Enable", orgAutoValidation.Enable.ToString());

        int CCSAdminEmailIdIndex = 0;
        foreach (var email in orgAutoValidation.CCSAdminEmailIds)
        {
          Data.Add($"OrgAutoValidation:CCSAdminEmailIds:{CCSAdminEmailIdIndex++}", email);
        }

        int buyerSuccessAdminRoleIndex = 0;
        foreach (var role in orgAutoValidation.BuyerSuccessAdminRoles)
        {
          Data.Add($"OrgAutoValidation:BuyerSuccessAdminRoles:{buyerSuccessAdminRoleIndex++}", role);
        }

        int bothSuccessAdminRoleIndex = 0;
        foreach (var role in orgAutoValidation.BothSuccessAdminRoles)
        {
          Data.Add($"OrgAutoValidation:BothSuccessAdminRoles:{bothSuccessAdminRoleIndex++}", role);
        }
      }
      if (_secrets.Data.ContainsKey("LookUpApiSettings"))
      {
        var wrapperApiKeySettings = JsonConvert.DeserializeObject<LookUpApiSettings>(_secrets.Data["LookUpApiSettings"].ToString());
        Data.Add("LookUpApiSettings:LookUpApiKey", wrapperApiKeySettings.LookUpApiKey);
        Data.Add("LookUpApiSettings:LookUpApiUrl", wrapperApiKeySettings.LookUpApiUrl);
      }
      if (_secrets.Data.ContainsKey("OrgAutoValidationEmail"))
      {
        var orgAutoValidationEmailInfo = JsonConvert.DeserializeObject<OrgAutoValidationEmailInfo>(_secrets.Data["OrgAutoValidationEmail"].ToString());
        Data.Add("OrgAutoValidationEmail:DeclineRightToBuyStatusEmailTemplateId", orgAutoValidationEmailInfo.DeclineRightToBuyStatusEmailTemplateId);
        Data.Add("OrgAutoValidationEmail:ApproveRightToBuyStatusEmailTemplateId", orgAutoValidationEmailInfo.ApproveRightToBuyStatusEmailTemplateId);
        Data.Add("OrgAutoValidationEmail:RemoveRightToBuyStatusEmailTemplateId", orgAutoValidationEmailInfo.RemoveRightToBuyStatusEmailTemplateId);
        Data.Add("OrgAutoValidationEmail:OrgPendingVerificationEmailTemplateId", orgAutoValidationEmailInfo.OrgPendingVerificationEmailTemplateId);
        Data.Add("OrgAutoValidationEmail:OrgBuyerStatusChangeUpdateToAllAdmins", orgAutoValidationEmailInfo.OrgBuyerStatusChangeUpdateToAllAdmins);

      }
      if (_secrets.Data.ContainsKey("UserRoleApproval"))
      {
        var roleApproval = JsonConvert.DeserializeObject<UserRoleApproval>(_secrets.Data["UserRoleApproval"].ToString());
        Data.Add("UserRoleApproval:Enable", roleApproval.Enable.ToString());
        Data.Add("UserRoleApproval:RoleApprovalTokenEncryptionKey", roleApproval.RoleApprovalTokenEncryptionKey);
        Data.Add("UserRoleApproval:UserRoleApprovalEmailTemplateId", roleApproval.UserRoleApprovalEmailTemplateId);
        Data.Add("UserRoleApproval:UserRoleApprovedEmailTemplateId", roleApproval.UserRoleApprovedEmailTemplateId);
        Data.Add("UserRoleApproval:UserRoleRejectedEmailTemplateId", roleApproval.UserRoleRejectedEmailTemplateId);
      }

      if (_secrets.Data.ContainsKey("ServiceRoleGroupSettings"))
      {
        var serviceRoleGroupSettings = JsonConvert.DeserializeObject<ServiceRoleGroupSettings>(_secrets.Data["ServiceRoleGroupSettings"].ToString());
        Data.Add("ServiceRoleGroupSettings:Enable", serviceRoleGroupSettings.Enable.ToString());
      }

      if (_secrets.Data.ContainsKey("NewUserJoinRequest"))
      {
        var serviceRoleGroupSettings = JsonConvert.DeserializeObject<NewUserJoinRequest>(_secrets.Data["NewUserJoinRequest"].ToString());
        Data.Add("NewUserJoinRequest:LinkExpirationInMinutes", serviceRoleGroupSettings.LinkExpirationInMinutes.ToString());
      }

      Data.Add("TokenEncryptionKey", _secrets.Data["TokenEncryptionKey"].ToString());
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

  public class JwtTokenValidationInfoVault
  {
    public string IdamClienId { get; set; }

    public string Issuer { get; set; }

    public string ApiGatewayEnabledJwksUrl { get; set; }

    public string ApiGatewayDisabledJwksUrl { get; set; }
  }

  public class SecurityApiSettingsVault
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class Email
  {
    public string ApiKey { get; set; }

    public string UserWelcomeEmailTemplateId { get; set; }

    public string OrgProfileUpdateNotificationTemplateId { get; set; }

    public string UserProfileUpdateNotificationTemplateId { get; set; }

    public string UserContactUpdateNotificationTemplateId { get; set; }

    public string UserPermissionUpdateNotificationTemplateId { get; set; }

    public string UserUpdateEmailOnlyFederatedIdpTemplateId { get; set; }
    public string UserUpdateEmailBothIdpTemplateId { get; set; }
    public string UserUpdateEmailOnlyUserIdPwdTemplateId { get; set; }

    public string UserConfirmEmailOnlyFederatedIdpTemplateId { get; set; }
    public string UserConfirmEmailBothIdpTemplateId { get; set; }
    public string UserConfirmEmailOnlyUserIdPwdTemplateId { get; set; }

    public string UserRegistrationEmailUserIdPwdTemplateId { get; set; }
    // #Delegated
    public string UserDelegatedAccessEmailTemplateId { get; set; }
    public string SendNotificationsEnabled { get; set; }
  }

  public class Cii
  {
    public string url { get; set; }
    public string token { get; set; }
    public string token_delete { get; set; }
    public string client_id { get; set; }
  }

  public class QueueInfoVault
  {
    public string AccessKeyId { get; set; } //AWSAccessKeyId

    public string AccessSecretKey { get; set; } //AWSAccessSecretKey

    public string ServiceUrl { get; set; } //AWSServiceUrl

    public string RecieveMessagesMaxCount { get; set; }

    public string RecieveWaitTimeInSeconds { get; set; }

    public string EnableAdaptorNotifications { get; set; }

    public string AdaptorNotificationQueueUrl { get; set; }
  }

  public class RedisCacheSettingsVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }

    public string CacheExpirationInMinutes { get; set; }
  }

  public class ExternalServiceDefaultRoles
  {
    public string[] GlobalServiceDefaultRoles { get; set; }

    public string[] ScopedServiceDefaultRoles { get; set; }
  }
  // #Delegated
  public class UserDelegation
  {
    public int DelegationEmailExpirationHours { get; set; }

    public string DelegationEmailTokenEncryptionKey { get; set; }

    public string[] DelegationExcludeRoles { get; set; }
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
