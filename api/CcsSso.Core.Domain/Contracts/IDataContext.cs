using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IDataContext
  {
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  }
}
