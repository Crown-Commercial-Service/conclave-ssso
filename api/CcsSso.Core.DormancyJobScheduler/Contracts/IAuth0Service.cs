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
    Task<UserDataList> GetUsersDataAsync(string q, int page, int perPage);

    Task UpdateUserStatusAsync(string userName, int status);
  }
}
