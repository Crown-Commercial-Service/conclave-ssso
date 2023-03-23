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

namespace CcsSso.Api.CustomOptions
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
      var vault = (JObject)JsonConvert.DeserializeObject<JObject>(env)["hashicorp-vault"][0];
      _vcapSettings = JsonConvert.DeserializeObject<VCapSettings>(vault.ToString());
      IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken: _vcapSettings.credentials.auth.token);
      var vaultClientSettings = new VaultClientSettings(_vcapSettings.credentials.address, authMethod);
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
      var _secrets = await _client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/core", mountPathValue);
      var _dbConnection = _secrets.Data["DbConnection"].ToString();
      var _isApiGatewayEnabled = _secrets.Data["IsApiGatewayEnabled"].ToString();
      var _cii = JsonConvert.DeserializeObject<Cii>(_secrets.Data["Cii"].ToString());
      Data.Add("DbConnection", _dbConnection);
      Data.Add("IsApiGatewayEnabled", _isApiGatewayEnabled);
      Data.Add("EnableAdditionalLogs", _secrets.Data["EnableAdditionalLogs"].ToString());
      Data.Add("CustomDomain", _secrets.Data["CustomDomain"].ToString());
      Data.Add("DashboardServiceClientId", _secrets.Data["DashboardServiceClientId"].ToString());
      Data.Add("BulkUploadMaxUserCount", _secrets.Data["BulkUploadMaxUserCount"].ToString());

      if (_secrets.Data.ContainsKey("CorsDomains"))
      {
        var corsList = JsonConvert.DeserializeObject<List<string>>(_secrets.Data["CorsDomains"].ToString());
        int index = 0;
        foreach (var cors in corsList)
        {
          Data.Add($"CorsDomains:{index++}", cors);
        }
      }
      Data.Add("Cii:Url", _cii.url);
      Data.Add("Cii:Token", _cii.token);
      Data.Add("Cii:Token_Delete", _cii.token_delete);

      if (_secrets.Data.ContainsKey("DocUpload"))
      {
        var docUploadInfo = JsonConvert.DeserializeObject<DocUploadInfo>(_secrets.Data["DocUpload"].ToString());
        Data.Add("DocUpload:Url", docUploadInfo.Url);
        Data.Add("DocUpload:Token", docUploadInfo.Token);
        Data.Add("DocUpload:SizeValidationValue", docUploadInfo.SizeValidationValue);
        Data.Add("DocUpload:TypeValidationValue", docUploadInfo.TypeValidationValue);
      }

      if (_secrets.Data.ContainsKey("Email"))
      {
        var emailInfo = JsonConvert.DeserializeObject<EmailInfoVault>(_secrets.Data["Email"].ToString());
        Data.Add("Email:NominateEmailTemplateId", emailInfo.NominateEmailTemplateId);
        Data.Add("Email:OrganisationJoinRequestTemplateId", emailInfo.OrganisationJoinRequestTemplateId);        
        Data.Add("Email:ApiKey", emailInfo.ApiKey);
        Data.Add("Email:UserConfirmEmailOnlyFederatedIdpTemplateId", emailInfo.UserConfirmEmailOnlyFederatedIdpTemplateId);
        Data.Add("Email:UserConfirmEmailBothIdpTemplateId", emailInfo.UserConfirmEmailBothIdpTemplateId);
        Data.Add("Email:UserConfirmEmailOnlyUserIdPwdTemplateId", emailInfo.UserConfirmEmailOnlyUserIdPwdTemplateId);
        Data.Add("Email:SendNotificationsEnabled", emailInfo.SendNotificationsEnabled);
      }

      if (_secrets.Data.ContainsKey("ConclaveSettings"))
      {
        var conclaveSettingsVault = JsonConvert.DeserializeObject<ConclaveSettingsVault>(_secrets.Data["ConclaveSettings"].ToString());
        Data.Add("ConclaveSettings:BaseUrl", conclaveSettingsVault.BaseUrl);
        Data.Add("ConclaveSettings:OrgRegistrationRoute", conclaveSettingsVault.OrgRegistrationRoute);
        Data.Add("ConclaveSettings:VerifyUserDetailsRoute", conclaveSettingsVault.VerifyUserDetailsRoute);
      }

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

      if (_secrets.Data.ContainsKey("S3ConfigurationInfo"))
      {
        var s3ConfigInfo = JsonConvert.DeserializeObject<S3ConfigurationInfoVault>(_secrets.Data["S3ConfigurationInfo"].ToString());
        Data.Add("S3ConfigurationInfo:AccessKeyId", s3ConfigInfo.AccessKeyId);
        Data.Add("S3ConfigurationInfo:AccessSecretKey", s3ConfigInfo.AccessSecretKey);
        Data.Add("S3ConfigurationInfo:ServiceUrl", s3ConfigInfo.ServiceUrl);
        Data.Add("S3ConfigurationInfo:BulkUploadBucketName", s3ConfigInfo.BulkUploadBucketName);
        Data.Add("S3ConfigurationInfo:BulkUploadFolderName", s3ConfigInfo.BulkUploadFolderName);
        Data.Add("S3ConfigurationInfo:BulkUploadTemplateFolderName", s3ConfigInfo.BulkUploadTemplateFolderName);
        Data.Add("S3ConfigurationInfo:FileAccessExpirationInHours", s3ConfigInfo.FileAccessExpirationInHours);
      }

      if (_secrets.Data.ContainsKey("RedisCacheSettings"))
      {
        var redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(_secrets.Data["RedisCacheSettings"].ToString());
        Data.Add("RedisCacheSettings:ConnectionString", redisCacheSettingsVault.ConnectionString);
        Data.Add("RedisCacheSettings:IsEnabled", redisCacheSettingsVault.IsEnabled);
        Data.Add("RedisCacheSettings:CacheExpirationInMinutes", redisCacheSettingsVault.CacheExpirationInMinutes);
      }

      // #Auto validation
      if (_secrets.Data.ContainsKey("OrgAutoValidation"))
      {
        var orgAutoValidation = JsonConvert.DeserializeObject<OrgAutoValidation>(_secrets.Data["OrgAutoValidation"].ToString());
        Data.Add("OrgAutoValidation:Enable", orgAutoValidation.Enable.ToString());
      }

      if (_secrets.Data.ContainsKey("WrapperApiSettings"))
      {
        var wrapperApiKeySettings = JsonConvert.DeserializeObject<WrapperApiSettingsVault>(_secrets.Data["WrapperApiSettings"].ToString());
        Data.Add("WrapperApiSettings:OrgApiKey", wrapperApiKeySettings.OrgApiKey);
        Data.Add("WrapperApiSettings:ApiGatewayEnabledOrgUrl", wrapperApiKeySettings.ApiGatewayEnabledOrgUrl); // Keep the trailing "/"
        Data.Add("WrapperApiSettings:ApiGatewayDisabledOrgUrl", wrapperApiKeySettings.ApiGatewayDisabledOrgUrl); // Keep the trailing "/"
      }

      if (_secrets.Data.ContainsKey("LookUpApiSettings"))
      {
        var wrapperApiKeySettings = JsonConvert.DeserializeObject<LookUpApiSettings>(_secrets.Data["LookUpApiSettings"].ToString());
        Data.Add("LookUpApiSettings:LookUpApiKey", wrapperApiKeySettings.LookUpApiKey);
        Data.Add("LookUpApiSettings:LookUpApiUrl", wrapperApiKeySettings.LookUpApiUrl);
      }

      if (_secrets.Data.ContainsKey("UserRoleApproval"))
      {
        var userRoleApproval = JsonConvert.DeserializeObject<UserRoleApproval>(_secrets.Data["UserRoleApproval"].ToString());
        Data.Add("UserRoleApproval:Enable", userRoleApproval.Enable.ToString());
      }

      Data.Add("TokenEncryptionKey", _secrets.Data["TokenEncryptionKey"].ToString());

      if (_secrets.Data.ContainsKey("NewUserJoinRequest"))
      {
        var newUserJoinRequest = JsonConvert.DeserializeObject<NewUserJoinRequest>(_secrets.Data["NewUserJoinRequest"].ToString());
        Data.Add("NewUserJoinRequest:LinkExpirationInMinutes", newUserJoinRequest.LinkExpirationInMinutes.ToString());
      }
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

  public class VaultOptions
  {
    public string Address { get; set; }
  }

  public class Cii
  {
    public string url { get; set; }
    public string token { get; set; }
    public string token_delete { get; set; }
    public string client_id { get; set; }
  }

  public class DocUploadInfo
  {
    public string Url { get; set; }

    public string Token { get; set; }

    public string SizeValidationValue { get; set; }

    public string TypeValidationValue { get; set; }
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

  public class EmailInfoVault
  {
    public string ApiKey { get; set; }

    public string NominateEmailTemplateId { get; set; }

    public string OrganisationJoinRequestTemplateId { get; set; }

    public string SendNotificationsEnabled { get; set; }

    public string UserConfirmEmailOnlyFederatedIdpTemplateId { get; set; }
    public string UserConfirmEmailBothIdpTemplateId { get; set; }
    public string UserConfirmEmailOnlyUserIdPwdTemplateId { get; set; }


  }

  public class ConclaveSettingsVault
  {
    public string BaseUrl { get; set; }

    public string OrgRegistrationRoute { get; set; }

    public string VerifyUserDetailsRoute { get; set; }
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

  public class S3ConfigurationInfoVault
  {
    public string AccessKeyId { get; set; }

    public string AccessSecretKey { get; set; }

    public string ServiceUrl { get; set; }

    public string BulkUploadBucketName { get; set; }

    public string BulkUploadTemplateFolderName { get; set; }

    public string BulkUploadFolderName { get; set; }

    public string FileAccessExpirationInHours { get; set; }
  }

  public class RedisCacheSettingsVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }

    public string CacheExpirationInMinutes { get; set; }
  }

  // #Auto validation
  public class WrapperApiSettingsVault
  {
    public string OrgApiKey { get; set; }

    public string ApiGatewayEnabledOrgUrl { get; set; }

    public string ApiGatewayDisabledOrgUrl { get; set; }
  }

  public class LookUpApiSettings
  {
    public string LookUpApiKey { get; set; }

    public string LookUpApiUrl { get; set; }
  }

  public class UserRoleApproval
  {
    public bool Enable { get; set; } = false;
  }

  public class NewUserJoinRequest
  {
    public int LinkExpirationInMinutes { get; set; }
  }

}
