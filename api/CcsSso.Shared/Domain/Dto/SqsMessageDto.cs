using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain.Dto
{
  public class SqsMessageDto
  {
    public string MessageBody { get; set; }

    public Dictionary<string, string> StringCustomAttributes { get; set; }

    public Dictionary<string, int> NumberCustomAttributes { get; set; }
  }

  public class SqsMessageResponseDto
  {
    public string MessageBody { get; set; }

    public Dictionary<string, string> StringCustomAttributes { get; set; }

    public Dictionary<string, int> NumberCustomAttributes { get; set; }

    public string MessageId { get; set; }

    public string ReceiptHandle { get; set; }

    public int ReceiveCount { get; set; }
  }
}
