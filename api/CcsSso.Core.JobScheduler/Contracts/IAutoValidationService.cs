using CcsSso.Core.JobScheduler.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IAutoValidationService
  {
    Task PerformJobAsync(List<OrganisationDetail> organisation);
  }
}