using System.Collections.Generic;

namespace CcsSso.Domain.Dtos
{
  public class ApplicationConfigurationInfo
  {
    public string ApiKey { get; set; }

    public ConclaveSettings ConclaveSettings { get; set; }

    public string ConclaveLoginUrl { get; set; }

    public bool EnableAdapterNotifications { get; set; }

    public int InMemoryCacheExpirationInMinutes { get; set; }

    public string DashboardServiceClientId { get; set; }

    public JwtTokenValidationConfigurationInfo JwtTokenValidationInfo { get; set; }

    public SecurityApiDetails SecurityApiDetails { get; set; }

    public CcsEmailInfo EmailInfo { get; set; }

    public QueueUrlInfo QueueUrlInfo { get; set; }

    public RedisCacheSetting RedisCacheSettings { get; set; }

    public string CustomDomain { get; set; }

    public ServiceDefaultRoleInfo ServiceDefaultRoleInfo { get; set; }

    public int BulkUploadMaxUserCount { get; set; }
    // #Delegated
    public int DelegationEmailExpirationHours { get; set; }

    public string DelegationEmailTokenEncryptionKey { get; set; }

    public string[] DelegationExcludeRoles { get; set; }

    // #Auto validation
    public OrgAutoValidation OrgAutoValidation { get; set; }
    public OrgAutoValidationEmailInfo OrgAutoValidationEmailInfo { get; set; }

    public UserRoleApproval UserRoleApproval { get; set; }
    public bool EnableUserAccessTokenFix { get; set; }

    public ServiceRoleGroupSettings ServiceRoleGroupSettings { get; set; }

    public string TokenEncryptionKey { get; set; }

    public NewUserJoinRequest NewUserJoinRequest { get; set; }
  }

  public class ServiceDefaultRoleInfo
  {
    public List<string> GlobalServiceDefaultRoles { get; set; }

    public List<string> ScopedServiceDefaultRoles { get; set; }
  }

  public class JwtTokenValidationConfigurationInfo
  {
    public string IdamClienId { get; set; }

    public string Issuer { get; set; }

    public string JwksUrl { get; set; }
  }

  public class SecurityApiDetails
  {
    public string Url { get; set; }

    public string ApiKey { get; set; }
  }

  public class ConclaveSettings
  {
    public string BaseUrl { get; set; }

    public string OrgRegistrationRoute { get; set; }

    public string VerifyUserDetailsRoute { get; set; }
  }

  public class CcsEmailInfo
  {
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

    public string OrganisationJoinRequestTemplateId { get; set; }

    public string NominateEmailTemplateId { get; set; }
    // #Delegated
    public string UserDelegatedAccessEmailTemplateId { get; set; }

    public bool SendNotificationsEnabled { get; set; }

  }

  public class QueueUrlInfo
  {
    public string AdaptorNotificationQueueUrl { get; set; }
  }

  public class RedisCacheSetting
  {
    public string ConnectionString { get; set; }

    public bool IsEnabled { get; set; }

    public int CacheExpirationInMinutes { get; set; }
  }

  public class DocUploadConfig
  {
    public string BaseUrl { get; set; }

    public string Token { get; set; }

    public int DefaultSizeValidationValue { get; set; }

    public string DefaultTypeValidationValue { get; set; }
  }

  // #Auto validation
  public class OrgAutoValidation
  {
    public bool Enable { get; set; } = false;
    public string[] CCSAdminEmailIds { get; set; }
    public string[] BuyerSuccessAdminRoles { get; set; }
    public string[] BothSuccessAdminRoles { get; set; }
  }

  public class ExternalApiDetails
  {
    public string Url { get; set; }

    public string ApiKey { get; set; }
  }

  public class LookUpApiSettings
  {
    public string LookUpApiKey { get; set; }

    public string LookUpApiUrl { get; set; }
  }

  public class OrgAutoValidationEmailInfo
  {
    public string DeclineRightToBuyStatusEmailTemplateId { get; set; }
    public string ApproveRightToBuyStatusEmailTemplateId { get; set; }
    public string RemoveRightToBuyStatusEmailTemplateId { get; set; }
    public string OrgPendingVerificationEmailTemplateId { get; set; }
    public string OrgBuyerStatusChangeUpdateToAllAdmins { get; set; }
  }

  public class UserRoleApproval
  {
    public bool Enable { get; set; } = false;

    public string RoleApprovalTokenEncryptionKey { get; set; }

    public string UserRoleApprovalEmailTemplateId { get; set; }

    public string UserRoleApprovedEmailTemplateId { get; set; }

    public string UserRoleRejectedEmailTemplateId { get; set; }
  }

  public class ServiceRoleGroupSettings
  {
    public bool Enable { get; set; } = false;
  }

  public class NewUserJoinRequest
  {
    public int LinkExpirationInMinutes { get; set; }
  }
}
