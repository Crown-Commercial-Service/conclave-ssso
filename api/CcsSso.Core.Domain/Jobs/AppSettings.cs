using CcsSso.Domain.Dtos;
using System.Collections.Generic;

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
    public OrgAutoValidationOneTimeJob? OrgAutoValidationOneTimeJob { get; set; }
    public OrgAutoValidationOneTimeJobEmail? OrgAutoValidationOneTimeJobEmail { get; set; }

    public OrgAutoValidationOneTimeJobRoles? OrgAutoValidationOneTimeJobRoles { get; set; }

    public ActiveJobStatus? ActiveJobStatus { get; set; }
    public bool IsApiGatewayEnabled { get; set; }
    public ServiceRoleGroupSettings ServiceRoleGroupSettings { get; set; }

    public NotificationApiSettings NotificationApiSettings { get; set; }
  }

  public class CiiSettings
  {
    public string Url { get; set; }

    public string Token { get; set; }
  }

	public class NotificationApiSettings
	{
		public string NotificationApiUrl { get; set; }
		public string NotificationApiKey { get; set; }
	}
	public class ActiveJobStatus
  {
    public bool RoleDeleteExpiredNotificationJob { get; set; }

  }

  public class ScheduleJobSettings
  {
    public int InactiveOrganisationDeletionJobExecutionFrequencyInMinutes { get; set; }

    public int UnverifiedUserDeletionJobExecutionFrequencyInMinutes { get; set; }

    public int OrganizationRegistrationExpiredThresholdInMinutes { get; set; }

    public int BulkUploadJobExecutionFrequencyInMinutes { get; set; }

    public int OrganisationAutovalidationJobExecutionFrequencyInMinutes { get; set; }
    public int RoleExpiredNotificationDeleteFrequencyInMinutes { get; set; }

  }
  public class WrapperApiSettings
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }

    public string ApiGatewayEnabledUserUrl { get; set; }

    public string ApiGatewayDisabledUserUrl   { get; set; }

    public string ConfigApiKey { get; set; }
    public string OrgApiKey { get; set; }
    public string OrgDeleteApiKey { get; set; }
    public string UserApiKey { get; set; }
    public string SecurityApiKey { get; set; }
    public string ContactApiKey { get; set; }
    public string ApiGatewayEnabledConfigUrl { get; set; }
    public string ApiGatewayEnabledOrgUrl { get; set; }
    public string ApiGatewayEnabledContactUrl { get; set; }
    public string ApiGatewayDisabledConfigUrl { get; set; }
    public string ApiGatewayDisabledOrgUrl { get; set; }
    public string ApiGatewayDisabledSecurityUrl { get; set; }
    public string ApiGatewayEnabledSecurityUrl { get; set; }
    public string ApiGatewayDisabledContactUrl { get; set; }
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

  public class OrgAutoValidationOneTimeJob
  {
    public bool Enable { get; set; } = false;
    public bool ReportingMode { get; set; } = false;
    public string StartDate { get; set; }

    public string EndDate { get; set; }

    public string LogReportEmailId { get; set; }

  }

  public class OrgAutoValidationOneTimeJobRoles
  {
    public string[] RemoveBuyerRoleFromSupplierOrg { get; set; }
    public string[] RemoveRoleFromAllOrg { get; set; }
    public string[] RemoveRoleFromBuyerOrg { get; set; }
    public string[] AddRolesToSupplierOrg { get; set; }
    public string[] AddRolesToBothOrgOnly { get; set; }

  }

  public class OrgAutoValidationOneTimeJobEmail
  {
    public string FailedAutoValidationNotificationTemplateId { get; set; }

  }
}
