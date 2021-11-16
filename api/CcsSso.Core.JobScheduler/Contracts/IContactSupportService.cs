using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IContactSupportService
  {
    Task<bool> IsOrgSiteContactExistsAsync(List<int> userContacPointIds, int organisationId);

    Task<bool> IsOtherUserContactExistsAsync(string userName, int organisationId);
  }
}
