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

        public int DelegatedEmailExpirationHours { get; set; }

        public string DelegationEmailTokenEncryptionKey { get; set; }
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
}
