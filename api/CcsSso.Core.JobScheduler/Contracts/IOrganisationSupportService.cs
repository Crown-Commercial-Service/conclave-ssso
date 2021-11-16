using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IOrganisationSupportService
  {
    Task<List<User>> GetAdminUsersAsync(int organisationId);
  }
}
