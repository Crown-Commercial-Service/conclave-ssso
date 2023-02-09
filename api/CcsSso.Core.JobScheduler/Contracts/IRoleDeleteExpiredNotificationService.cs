using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IRoleDeleteExpiredNotificationService
  {
    Task PerformJobAsync(List<UserAccessRolePending> organisation);
  }
}