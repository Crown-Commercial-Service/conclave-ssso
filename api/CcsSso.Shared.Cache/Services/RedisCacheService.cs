using CcsSso.Shared.Cache.Contracts;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Cache.Services
{
  public class RedisCacheService : IRemoteCacheService
  {
    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
    {
      TypeNameHandling = TypeNameHandling.Auto,
      ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private RedisConnectionPoolService _redisConnectionService;
    private string _baseKeyPath = string.Empty;

    public RedisCacheService(RedisConnectionPoolService redisConnectionService)
    {
      _redisConnectionService = redisConnectionService;
    }

    private IDatabase RedisDatabase
    {
      get
      {
        return _redisConnectionService.GetDatabase();
      }
    }

    public TValue GetValue<TValue>(string key)
    {
      var value = RedisDatabase.StringGet(GetFullKey(key));
      return Deserialize<TValue>(value);
    }

    public async Task<TValue> GetValueAsync<TValue>(string key)
    {
      var value = await RedisDatabase.StringGetAsync(GetFullKey(key));
      return Deserialize<TValue>(value);
    }

    public void SetValue<TValue>(string key, TValue value)
    {
      RedisDatabase.StringSet(GetFullKey(key), Serialize(value));
    }

    public async Task SetValueAsync<TValue>(string key, TValue value)
    {
      await RedisDatabase.StringSetAsync(GetFullKey(key), Serialize(value));
    }

    public void SetValue<TValue>(string key, TValue value, TimeSpan expiration)
    {
      RedisDatabase.StringSet(GetFullKey(key), Serialize(value), expiration);
    }

    public async Task SetValueAsync<TValue>(string key, TValue value, TimeSpan expiration)
    {
      await RedisDatabase.StringSetAsync(GetFullKey(key), Serialize(value), expiration);
    }

    public void Remove(params string[] keys)
    {
      foreach (var key in keys)
      {
        RedisDatabase.KeyDelete(GetFullKey(key));
      }
    }

    public async Task RemoveAsync(params string[] keys)
    {
      if (keys.Length == 1)
      {
        await RedisDatabase.KeyDeleteAsync(GetFullKey(keys[0]));
      }
      else
      {
        var tasks = new List<Task>();
        foreach (var key in keys)
        {
          tasks.Add(RedisDatabase.KeyDeleteAsync(GetFullKey(key)));
        }

        await Task.WhenAll(tasks);
      }
    }

    public bool KeyExists(string key)
    {
      return RedisDatabase.KeyExists(key);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
      return await RedisDatabase.KeyExistsAsync(key);
    }

    // This should be moved to default interface implementation when C# 8.0 is available.
    // https://github.com/dotnet/csharplang/issues/288
    public async Task<TValue> GetOrSetValueAsync<TValue>(string key, Func<Task<TValue>> asyncResolver, int? expirationInMinutes = null)
    {
      var value = await GetValueAsync<TValue>(key);
      if (value == null)
      {
        value = await asyncResolver();
        if (!expirationInMinutes.HasValue)
        {
          await SetValueAsync(key, value);
        }
        else
        {
          await SetValueAsync(key, value, new TimeSpan(0, expirationInMinutes.Value, 0));
        }
      }

      return value;
    }

    private string Serialize(object obj)
    {
      if (obj == null)
      {
        return null;
      }

      // Using Json serialization for convenience + performance
      return JsonConvert.SerializeObject(obj, _jsonSettings);
    }

    private T Deserialize<T>(string str)
    {
      if (str == null)
      {
        return default(T);
      }

      // Using Json serialization for convenience + performance
      return JsonConvert.DeserializeObject<T>(str, _jsonSettings);
    }

    /// <summary>
    /// Returns the key with base path appended to it.
    /// </summary>
    private string GetFullKey(string key)
    {
      return $"{_baseKeyPath}/{key}";
    }
  }
}
