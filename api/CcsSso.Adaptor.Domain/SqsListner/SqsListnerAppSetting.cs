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
  }

  public class SqsListnerJobSetting
  {
    public int JobSchedulerExecutionFrequencyInMinutes { get; set; }

    public int MessageReadThreshold { get; set; }
  }

  public class AdaptorApiSetting
  {
    public string ApiGatewayEnabledUrl { get; set; }

    public string ApiGatewayDisabledUrl { get; set; }

    public string ApiKey { get; set; }
  }

  public class QueueUrlInfo
  {
    public string AdapterNotificationQueueUrl { get; set; }

    public string PushDataQueueUrl { get; set; }
  }

  public class SqsListnerJobSettingVault
  {
    public string JobSchedulerExecutionFrequencyInMinutes { get; set; }

    public string MessageReadThreshold { get; set; }
  }

  public class QueueInfoVault
  {
    public string AccessKeyId { get; set; } //AWSAccessKeyId

    public string AccessSecretKey { get; set; } //AWSAccessSecretKey

    public string ServiceUrl { get; set; } //AWSServiceUrl

    public string RecieveMessagesMaxCount { get; set; }

    public string RecieveWaitTimeInSeconds { get; set; }

    public string AdapterNotificationQueueUrl { get; set; }

    public string PushDataQueueUrl { get; set; }
  }

  public class RedisCacheSettingVault
  {
    public string ConnectionString { get; set; }

    public string IsEnabled { get; set; }
  }
}
