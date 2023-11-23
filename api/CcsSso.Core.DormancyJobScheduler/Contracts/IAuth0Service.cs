using CcsSso.Core.DormancyJobScheduler.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Contracts
{
  public interface IAuth0Service
  {
    Task<UserListDetails> GetUsersByLastLogin(string fromDate, string toDate, int page, int perPage);
  }
}
