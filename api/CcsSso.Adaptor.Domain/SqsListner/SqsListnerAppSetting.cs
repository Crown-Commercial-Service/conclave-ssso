using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.SqsListener
{
  public class SqsListnerAppSetting
  {
    public SqsListnerJobSetting SqsListnerJobSetting { get; set; }

    public QueueUrlInfo QueueUrlInfo { get; set; }

    public DataQueueSettings DataQueueSettings { get; set; }

    public SecurityApiSettings SecurityApiSettings { get; set; }

    public EmailSettings EmailSettings { get; set; }
  }

  public class SqsListnerJobSetting
  {
    public int JobSchedulerExecutionFrequencyInMinutes { get; set; }

    public int MessageReadThreshold { get; set; }

    public int DataQueueJobSchedulerExecutionFrequencyInMinutes { get; set; }

    public int DataQueueMessageReadThreshold { get; set; }
  }

  public class AdaptorApiSetting
  {
    public string ApiGatewayEnabledUrl { get; set; }

    public string ApiGatewayDisabledUrl { get; set; }

    public string ApiKey { get; set; }
  }

  public class QueueUrlInfo
  {
    public string AdaptorNotificationQueueUrl { get; set; }

    public string PushDataQueueUrl { get; set; }

    public string DataQueueUrl { get; set; }
  }

  public class SqsListnerJobSettingVault
  {
    public string JobSchedulerExecutionFrequencyInMinutes { get; set; }

    public string MessageReadThreshold { get; set; }

    public string DataQueueJobSchedulerExecutionFrequencyInMinutes { get; set; }

    public string DataQueueMessageReadThreshold { get; set; }
  }

  public class QueueInfoVault
  {
    public string AccessKeyId { get; set; } //AWSAccessKeyId
    public string AccessSecretKey { get; set; } //AWSAccessSecretKey

    public string AdaptorNotificationAccessKeyId { get; set; }
    public string AdaptorNotificationAccessSecretKey { get; set; }
    public string PushDataAccessKeyId { get; set; }
    public string PushDataAccessSecretKey { get; set; }

    public string ServiceUrl { get; set; } //AWSServiceUrl

    public string RecieveMessagesMaxCount { get; set; }

    public string RecieveWaitTimeInSeconds { get; set; }

    public string AdaptorNotificationQueueUrl { get; set; }

    public string PushDataQueueUrl { get; set; }

    public string DataQueueRecieveMessagesMaxCount { get; set; }

    public string DataQueueRecieveWaitTimeInSeconds { get; set; }

    public string DataQueueUrl { get; set; }

    public string DataQueueAccessKeyId { get; set; }

    public string DataQueueAccessSecretKey { get; set; }
  }

  public class RedisCacheSettingVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }
  }
  public class SecurityApiSettingsVault
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class SecurityApiSettings
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class DataQueueSettingsVault
  {
    public string DelayInSeconds { get; set; }

    public string RetryMaxCount { get; set; }
  }

  public class DataQueueSettings
  {
    public int DelayInSeconds { get; set; }

    public int RetryMaxCount { get; set; }
  }

  public class EmailSettingsVault
  {
    public string ApiKey { get; set; }

    public string SendNotificationsEnabled { get; set; }

    public string Auth0CreateUserErrorNotificationTemplateId { get; set; }

    public string Auth0DeleteUserErrorNotificationTemplateId { get; set; }

    public string[] SendDataQueueErrorNotificationToEmailIds { get; set; }
  }

  public class EmailSettings
  {
    public string ApiKey { get; set; }

    public bool SendNotificationsEnabled { get; set; }

    public string Auth0CreateUserErrorNotificationTemplateId { get; set; }

    public string Auth0DeleteUserErrorNotificationTemplateId { get; set; }

    public string[] SendDataQueueErrorNotificationToEmailIds { get; set; }
  }
}
