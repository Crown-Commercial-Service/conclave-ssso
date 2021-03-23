using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Jobs.Services
{
  public class ScheduledJobServiceFactory : IScheduledJobServiceFactory
  {
    private readonly List<IJob> _jobs;
    public ScheduledJobServiceFactory(List<IJob> jobs)
    {
      _jobs = jobs;
    }

    public List<IJob> GetScheduledJobs()
    {
      return _jobs;
    }
  }
}
