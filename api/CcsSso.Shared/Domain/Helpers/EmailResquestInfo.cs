namespace CcsSso.Shared.Domain.Helpers
{
  public class EmailResquestInfo
  {
    public EmailInfo EmailInfo { get; set; }
    public bool IsUserInAuth0 { get; set; }
  }
}
