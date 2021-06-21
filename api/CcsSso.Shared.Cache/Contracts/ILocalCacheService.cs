using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Cache.Contracts
{
  public interface ILocalCacheService
  {
    TValue GetValue<TValue>(string key);

    void SetValue<TValue>(string key, TValue value);

    void SetValue<TValue>(string key, TValue value, TimeSpan expiration);

    void Remove(params string[] keys);

    bool KeyExists(string key);

    Task<TValue> GetOrSetValueAsync<TValue>(string key, Func<Task<TValue>> asyncResolver, int? expirationInMinutes = null);
  }
}
