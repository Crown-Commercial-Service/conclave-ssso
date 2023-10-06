namespace CcsSso.Core.DataMigrationJobScheduler.Contracts
{
  public  interface IFileUploadJobService
  {
    Task PerformFileUploadJobAsync();
  }
}
