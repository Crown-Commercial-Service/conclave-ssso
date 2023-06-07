using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DelegationJobScheduler.Contracts
{
  public  interface IDelegationExpiryNotificationService
  {
    Task PerformNotificationExpiryJobAsync();
  }
}
