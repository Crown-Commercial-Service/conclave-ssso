using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Jobs
{
  public class AppSettings
  {
    public string DbConnection { get; set; }

    public ScheduleJobSettings ScheduleJobSettings { get; set; }

    public CiiSettings CiiSettings { get; set; }

    public WrapperApiSettings WrapperApiSettings { get; set; }
    public SecurityApiSettings SecurityApiSettings { get; set; }

    public List<UserDeleteJobSetting> UserDeleteJobSettings { get; set; }

    public BulkUploadSettings BulkUploadSettings { get; set; }
    
    public OrgAutoValidationJobSettings OrgAutoValidationJobSettings { get; set; }
  }

  public class CiiSettings
  {
    public string Url { get; set; }

    public string Token { get; set; }
  }

  public class ScheduleJobSettings
  {
    public int InactiveOrganisationDeletionJobExecutionFrequencyInMinutes { get; set; }

    public int UnverifiedUserDeletionJobExecutionFrequencyInMinutes { get; set; }

    public int OrganizationRegistrationExpiredThresholdInMinutes { get; set; }

    public int BulkUploadJobExecutionFrequencyInMinutes { get; set; }

    public int OrganisationAutovalidationJobExecutionFrequencyInMinutes { get; set; }

  }
  public class WrapperApiSettings
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class SecurityApiSettings
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class UserDeleteJobSetting
  {
    public string ServiceClientId { get; set; }

    public int UserDeleteThresholdInMinutes { get; set; }

    public bool NotifyOrgAdmin { get; set; }

    public string AdminNotifyTemplateId { get; set; }
  }

  public class RedisCacheSettingsVault
  {
    public string ConnectionString { get; set; }
  }

  public class DocUploadInfoVault
  {
    public string Url { get; set; }

    public string Token { get; set; }

    public string SizeValidationValue { get; set; }

    public string TypeValidationValue { get; set; }
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

  public class BulkUploadSettings
  {
    public string BulkUploadReportUrl { get; set; }
  }

  // #Auto validation
  public class OrgAutoValidationJobSettings
  {
    public bool Enable { get; set; } = false;
  }
}
