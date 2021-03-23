using CcsSso.Core.Domain.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Jobs
{
  public interface IScheduledJobServiceFactory
  {
    List<IJob> GetScheduledJobs();
  }
}
