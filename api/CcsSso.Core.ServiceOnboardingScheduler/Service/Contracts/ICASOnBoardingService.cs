using CcsSso.Core.ServiceOnboardingScheduler.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ServiceOnboardingScheduler.Service.Contracts
{
  public interface ICASOnBoardingService
  {
    Task<List<Organisation>> GetRegisteredOrgsIds(bool ranOnce, DateTime startDate, DateTime dateTime);
    Task<List<Tuple<int, string, string, DateTime>>> GetOrgAdmins(int orgId, string ciiOrganisationId);
  }
}
