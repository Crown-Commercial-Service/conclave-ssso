namespace CcsSso.Domain.Dtos
{
  public class ApplicationConfigurationInfo
  {
    public string ApiKey { get; set; }

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

  public class CcsEmailInfo
  {
    public string UserWelcomeEmailTemplateId { get; set; }

    public string OrgProfileUpdateNotificationTemplateId { get; set; }

    public string UserProfileUpdateNotificationTemplateId { get; set; }

    public string UserContactUpdateNotificationTemplateId { get; set; }

    public string UserPermissionUpdateNotificationTemplateId { get; set; }

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
}
