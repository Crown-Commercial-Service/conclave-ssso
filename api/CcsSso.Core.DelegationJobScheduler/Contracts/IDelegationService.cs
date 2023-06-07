namespace CcsSso.Core.DelegationJobScheduler.Contracts
{
  public interface IDelegationService
  {
    Task PerformLinkExpireJobAsync();
    Task PerformDelegationTermissionJobAsync();
  }
}
