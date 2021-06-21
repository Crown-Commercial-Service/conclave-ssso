using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IWrapperCacheService
  {
    Task RemoveCacheAsync(params string[] keys);
  }
}
