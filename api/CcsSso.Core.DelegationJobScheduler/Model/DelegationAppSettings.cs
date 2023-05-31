using Microsoft.EntityFrameworkCore.Metadata;

namespace CcsSso.Core.DelegationJobScheduler.Model
{
  public class DelegationAppSettings
  {
    public string DbConnection { get; set; }
    public  string ConclaveLoginUrl { get; set; }
    public DelegationJobSettings DelegationJobSettings { get; set; }
    public DelegationExpiryNotificationJobSettings DelegationExpiryNotificationJobSettings { get; set; }
    public EmailSettings EmailSettings { get; set; }
  }

  public class DelegationJobSettings
  {
    public int DelegationTerminationJobFrequencyInMinutes { get; set; }
    public int DelegationLinkExpiryJobFrequencyInMinutes { get; set; }
  }

  public class DelegationExpiryNotificationJobSettings
  {
    public int JobFrequencyInMinutes { get; set; }
    public int ExpiryNoticeInMinutes { get; set; }
  }

  public class EmailSettings
  {
    public string ApiKey { get; set; }
    public string DelegationExpiryNotificationToAdminTemplateId { get; set; }
    public string DelegationExpiryNotificationToUserTemplateId { get; set; }
  }

}
