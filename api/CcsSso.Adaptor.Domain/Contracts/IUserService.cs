using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts
{
  public interface IUserService
  {
    Task<Dictionary<string, object>> GetUserAsync(string userName);
  }
}
