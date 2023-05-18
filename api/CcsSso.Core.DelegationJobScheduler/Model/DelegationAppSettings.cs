namespace CcsSso.Core.DelegationJobScheduler.Model
{
  public class DelegationAppSettings
  {
    public string DbConnection { get; set; }
    public DelegationJobSettings DelegationJobSettings { get; set; }
  }

  public class DelegationJobSettings
  {
    public int DelegationTerminationJobFrequencyInMinutes { get; set; }
    public int DelegationLinkExpiryJobFrequencyInMinutes { get; set; }
  }
}
