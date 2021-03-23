using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.Jobs.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Jobs.Services
{
  public class JobServiceManager : IJobServiceManager
  {
    private readonly IScheduledJobServiceFactory _scheduledJobServiceFactory;
    public JobServiceManager(IScheduledJobServiceFactory scheduledJobServiceFactory)
    {
      _scheduledJobServiceFactory = scheduledJobServiceFactory;
    }

    public async Task PerformJobAsync()
    {
      var jobs = _scheduledJobServiceFactory.GetScheduledJobs();
      foreach (var job in jobs)
      {
        await job.PerformJobAsync();
      }
    }
  }
}
