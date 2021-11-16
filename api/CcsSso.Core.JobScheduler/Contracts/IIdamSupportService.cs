using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IIdamSupportService
  {
    Task DeleteUserInIdamAsync(string userName);
  }
}
