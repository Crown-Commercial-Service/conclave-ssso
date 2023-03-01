using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Core.JobScheduler
{
  public class ProgramHelpers
  {
    private readonly IAwsParameterStoreService _awsParameterStoreService;
    private static string path = "/conclave-sso/org-dereg-job/";

    public ProgramHelpers()
    {
      _awsParameterStoreService =new AwsParameterStoreService();
    }


    public dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;
      if (objType == typeof(CiiSettings))
      {
        returnParams = new CiiSettings()
        {
          Token = _awsParameterStoreService.FindParameterByName(parameters, path + "CIISettings/Token"),
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "CIISettings/Url")
        };
      }
      else if (objType == typeof(List<UserDeleteJobSetting>))
      {
        // Aws value will be like this ANY|12960000|false,|12960000|true
        string value = _awsParameterStoreService.FindParameterByName(parameters, path + "UserDeleteJobSettings");
        if (!string.IsNullOrWhiteSpace(value))
        {
          returnParams = FillUserDeleteJobSetting(value);
        }
      }
      else if (objType == typeof(EmailConfigurationInfo))
      {
        returnParams = new EmailConfigurationInfo()
        {
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ApiKey"),
          BulkUploadReportTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/BulkUploadReportTemplateId"),
          UnverifiedUserDeletionNotificationTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UnverifiedUserDeletionNotificationTemplateId"),
          UserRoleExpiredEmailTemplateId= _awsParameterStoreService.FindParameterByName(parameters, path + "Email/UserRoleExpiredEmailTemplateId"),
        };
      }
      else if (objType == typeof(WrapperApiSettings))
      {
        returnParams = new WrapperApiSettings()
        {
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/Url"),
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiKey"),
          ApiGatewayEnabledUserUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayEnabledUserUrl"),
          ApiGatewayDisabledUserUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayDisabledUserUrl")
        };
      }
      else if (objType == typeof(SecurityApiSettings))
      {
        returnParams = new SecurityApiSettings()
        {
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/ApiKey"),
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/Url"),
        };
      }
      else if (objType == typeof(ScheduleJobSettings))
      {
        returnParams = new ScheduleJobSettings()
        {
          BulkUploadJobExecutionFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/BulkUploadJobExecutionFrequencyInMinutes")),
          InactiveOrganisationDeletionJobExecutionFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/InactiveOrganisationDeletionJobExecutionFrequencyInMinutes")),
          OrganizationRegistrationExpiredThresholdInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/OrganizationRegistrationExpiredThresholdInMinutes")),
          UnverifiedUserDeletionJobExecutionFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/UnverifiedUserDeletionJobExecutionFrequencyInMinutes")),
          OrganisationAutovalidationJobExecutionFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/OrganisationAutovalidationJobExecutionFrequencyInMinutes")),
          RoleExpiredNotificationDeleteFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/RoleExpiredNotificationDeleteFrequencyInMinutes"))
        };
      }
      else if (objType == typeof(BulkUploadSettings))
      {
        returnParams = new BulkUploadSettings()
        {
          BulkUploadReportUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "BulkUploadSettings/BulkUploadReportUrl")
        };
      }
      else if (objType == typeof(RedisCacheSettingsVault))
      {
        var redisCacheName = _awsParameterStoreService.FindParameterByName(parameters, path + "RedisCacheSettings/Name");
        var redisCacheConnectionString = _awsParameterStoreService.FindParameterByName(parameters, path + "RedisCacheSettings/ConnectionString");
        string dynamicRedisCacheConnectionString = null;

        if (!string.IsNullOrEmpty(redisCacheName))
        {
          dynamicRedisCacheConnectionString = UtilityHelper.GetRedisCacheConnectionString(redisCacheName, redisCacheConnectionString);
        }

        returnParams = new RedisCacheSettingsVault()
        {
          ConnectionString = dynamicRedisCacheConnectionString != null ? dynamicRedisCacheConnectionString : redisCacheConnectionString
        };
      }
      else if (objType == typeof(DocUploadInfoVault))
      {
        returnParams = new DocUploadInfoVault()
        {
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/Url"),
          SizeValidationValue = _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/SizeValidationValue"),
          Token = _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/Token"),
          TypeValidationValue = _awsParameterStoreService.FindParameterByName(parameters, path + "DocUpload/TypeValidationValue")
        };
      }
      else if (objType == typeof(S3ConfigurationInfoVault))
      {
        var s3ConfigurationInfoName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/Name");

        var AccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/AccessKeyId");
        var AccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/AccessSecretKey");

        if (!string.IsNullOrEmpty(s3ConfigurationInfoName))
        {
          var s3ConfigurationInfo = UtilityHelper.GetS3Settings(s3ConfigurationInfoName);
          AccessKeyId= s3ConfigurationInfo.credentials.aws_access_key_id;
          AccessSecretKey =s3ConfigurationInfo.credentials.aws_secret_access_key;
        }

        returnParams = new S3ConfigurationInfoVault()
        {
          AccessKeyId = AccessKeyId,
          AccessSecretKey = AccessSecretKey,
          ServiceUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/ServiceUrl"),
          BulkUploadBucketName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/BulkUploadBucketName"),
          BulkUploadTemplateFolderName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/BulkUploadTemplateFolderName"),
          BulkUploadFolderName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/BulkUploadFolderName"),
          FileAccessExpirationInHours = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/FileAccessExpirationInHours"),
        };
      }
      else if (objType == typeof(OrgAutoValidationJobSettings))
      {
        returnParams = new OrgAutoValidationJobSettings()
        {
          Enable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidation/Enable")),
        };
      }
      else if (objType == typeof(OrgAutoValidationOneTimeJob))
      {
        returnParams = new OrgAutoValidationOneTimeJob()
        {
          Enable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/Enable")),
          ReportingMode = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/ReportingMode")),
          StartDate = _awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/StartDate"),
          EndDate = _awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/EndDate"),
          LogReportEmailId = _awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/LogReportEmailId")
        };
      }
      else if (objType == typeof(OrgAutoValidationOneTimeJobRoles))
      {
        returnParams = new OrgAutoValidationOneTimeJobRoles()
        {
          AddRolesToBothOrgOnly = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobRoles/AddRolesToBothOrgOnly")),
          AddRolesToSupplierOrg = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobRoles/AddRolesToSupplierOrg")),
          RemoveBuyerRoleFromSupplierOrg = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobRoles/RemoveBuyerRoleFromSupplierOrg")),
          RemoveRoleFromAllOrg = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobRoles/RemoveRoleFromAllOrg")),
          RemoveRoleFromBuyerOrg = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobRoles/RemoveRoleFromBuyerOrg"))
        };
      }
      else if (objType == typeof(OrgAutoValidationOneTimeJobEmail))
      {
        returnParams = new OrgAutoValidationOneTimeJobEmail()
        {
          FailedAutoValidationNotificationTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobEmail/FailedAutoValidationNotificationTemplateId"),
        };
      }
      else if (objType == typeof(ActiveJobStatus))
      {
        returnParams = new ActiveJobStatus()
        {
          RoleDeleteExpiredNotificationJob=Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "ActiveJobStatus/RoleDeleteExpiredNotificationJob"))
        };
      }
      else if (objType == typeof(ServiceRoleGroupSettings))
      {
        returnParams = new ServiceRoleGroupSettings()
        {
          Enable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "ServiceRoleGroupSettings/Enable"))
        };
      }

      return returnParams;
    }

    private List<UserDeleteJobSetting> FillUserDeleteJobSetting(string value)
    {
      var settings = new List<UserDeleteJobSetting>();
      List<string> items = value.Split(',').ToList();

      foreach (var item in items)
      {
        List<string> itemValues = item?.Split('|').ToList();
        if (itemValues.Any())
        {
          settings.Add(new UserDeleteJobSetting()
          {
            ServiceClientId = itemValues.Count() >= 1 ? itemValues[0] : String.Empty,
            UserDeleteThresholdInMinutes = itemValues.Count() >= 2 ? Convert.ToInt32(itemValues[1]) : 0,
            NotifyOrgAdmin = itemValues.Count() >= 3 ? Convert.ToBoolean(itemValues[2]) : false,
          });
        }
      }
      return settings;
    }

    private string[] getStringToArray(string param)
    {
      if (param != null)
      {
        return param.Split(',').ToArray();
      }
      return Array.Empty<string>();
    }

    public async Task<List<Parameter>> LoadAwsSecretsAsync(IAwsParameterStoreService _awsParameterStoreService )
    {
      return await _awsParameterStoreService.GetParameters(path);
    }

    public async Task<Dictionary<string, object>> LoadSecretsAsync()
    {
      var env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var vault = (JObject)JsonConvert.DeserializeObject<JObject>(env)["hashicorp-vault"][0];
      var vcapSettings = JsonConvert.DeserializeObject<VCapSettings>(vault.ToString());

      IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken: vcapSettings.credentials.auth.token);
      var vaultClientSettings = new VaultClientSettings(vcapSettings.credentials.address, authMethod)
      {
        ContinueAsyncTasksOnCapturedContext = false
      };
      var client = new VaultClient(vaultClientSettings);
      var mountPathValue = vcapSettings.credentials.backends_shared.space.Split("/secret").FirstOrDefault();
      var _secrets = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/org-dereg-job", mountPathValue);
      return _secrets.Data;
    }
  }
}