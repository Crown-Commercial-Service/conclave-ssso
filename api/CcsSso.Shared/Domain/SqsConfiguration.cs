using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class SqsConfiguration
  {
    public string ServiceUrl { get; set; }

    public string AccessKeyId { get; set; }

    public string AccessSecretKey { get; set; }

    public string AdaptorNotificationAccessKeyId { get; set; }
    public string AdaptorNotificationAccessSecretKey { get; set; }
    public string PushDataAccessKeyId { get; set; }
    public string PushDataAccessSecretKey { get; set; }

    public int RecieveMessagesMaxCount { get; set; }

    public int RecieveWaitTimeInSeconds { get; set; }
  }
}
