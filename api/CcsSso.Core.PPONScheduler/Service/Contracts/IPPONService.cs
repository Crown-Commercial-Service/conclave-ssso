using CcsSso.Core.PPONScheduler.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.PPONScheduler.Service.Contracts
{
  public interface IPPONService
  {
    Task PerformJob(bool oneTimeValidationSwitch, DateTime startDate, DateTime endDate);
  }
}
