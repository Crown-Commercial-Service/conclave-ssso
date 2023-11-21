﻿using Microsoft.EntityFrameworkCore.Metadata;

namespace CcsSso.Core.DormancyJobScheduler.Model
{
  public class DormancyAppSettings
  {
    public DormancyJobSettings DormancyJobSettings { get; set; }
    public WrapperApiSettings WrapperApiSettings { get; set; }
    public SecurityApiSettings SecurityApiSettings { get; set; }
    public EmailSettings EmailSettings { get; set; }
    public NotificationApiSettings NotificationApiSettings { get; set; }
    public bool IsApiGatewayEnabled { get; set; }
  }

  public class WrapperApiSettings
  {
    public string UserApiKey { get; set; }
    public string ApiGatewayEnabledUserUrl { get; set; }
    public string ApiGatewayDisabledUserUrl { get; set; }
  }

  public class DormancyJobSettings
  {
    public int DormancyNotificationJobFrequencyInMinutes { get; set; }
    public int UserDeactivationJobFrequencyInMinutes { get; set; }
    public int DeactivationNotificationInMinutes { get; set; }
    public int UserDeactivationDurationInMinutes { get; set; }

  }

  public class SecurityApiSettings
  {
    public string ApiKey { get; set; }
    public string Url { get; set; }
  }
  public class EmailSettings
  {
    public string ApiKey { get; set; }
    public string UserDormantNotificationTemplateId { get; set; }
  }
  public class NotificationApiSettings
  {
    public string NotificationApiUrl { get; set; }
    public string NotificationApiKey { get; set; }
  }
}
