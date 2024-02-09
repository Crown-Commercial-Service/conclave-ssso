namespace CcsSso.Core.DormancyJobScheduler.Contracts
{
  public interface IUserDeactivationService
  {
    Task PerformUserDeactivationJobAsync();
  }
}
