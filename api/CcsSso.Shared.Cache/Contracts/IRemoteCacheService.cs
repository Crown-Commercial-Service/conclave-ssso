using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Cache.Contracts
{
  public interface IRemoteCacheService
  {
    Task<TValue> GetValueAsync<TValue>(string key);

    Task SetValueAsync<TValue>(string key, TValue value);

    Task SetValueAsync<TValue>(string key, TValue value, TimeSpan expiration);

    Task RemoveAsync(params string[] keys);
  }
}
