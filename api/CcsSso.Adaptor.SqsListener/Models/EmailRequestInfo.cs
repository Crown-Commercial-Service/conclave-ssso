using CcsSso.Shared.Domain;

namespace CcsSso.Adaptor.SqsListener.Models
{
  public class EmailRequestInfo
  {
    public EmailInfo EmailInfo { get; set; }
    public bool IsUserInAuth0 { get; set; }
    public bool? isMessageRetry { get; set; }
  }
}
