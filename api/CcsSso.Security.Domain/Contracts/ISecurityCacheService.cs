using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface ISecurityCacheService
  {
    Task<TValue> GetValueAsync<TValue>(string key);
    Task SetValueAsync<TValue>(string key, TValue value, TimeSpan expiration);
    Task RemoveAsync(params string[] keys);
  }
}
