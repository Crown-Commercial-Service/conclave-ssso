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

    public int RecieveMessagesMaxCount { get; set; }

    public int RecieveWaitTimeInSeconds { get; set; }
  }
}
