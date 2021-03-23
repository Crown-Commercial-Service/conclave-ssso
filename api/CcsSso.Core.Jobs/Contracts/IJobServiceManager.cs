using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Jobs.Contracts
{
  public interface IJobServiceManager
  {
    Task PerformJobAsync();
  }
}
