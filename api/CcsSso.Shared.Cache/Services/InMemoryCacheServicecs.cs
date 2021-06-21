using CcsSso.Shared.Cache.Contracts;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace CcsSso.Shared.Cache.Services
{
  public class InMemoryCacheService : ILocalCacheService
    {
        private readonly IMemoryCache _memoryCache;

        public InMemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public TValue GetValue<TValue>(string key)
        {
            return _memoryCache.Get<TValue>(key);
        }

        public void Remove(params string[] keys)
        {
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }
        }

        public void SetValue<TValue>(string key, TValue value)
        {
            _memoryCache.Set(key, value);
        }

        public void SetValue<TValue>(string key, TValue value, TimeSpan expiration)
        {
            _memoryCache.Set(key, value, expiration);
        }

        public bool KeyExists(string key)
        {
            throw new NotImplementedException();
        }

        public async Task<TValue> GetOrSetValueAsync<TValue>(string key, Func<Task<TValue>> asyncResolver, int? expirationInMinutes = null)
        {
            var result = await _memoryCache.GetOrCreateAsync(key, async
                 entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(3600);
                return await asyncResolver();
            });
            return result;
        }
    }
}
