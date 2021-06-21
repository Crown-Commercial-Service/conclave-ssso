using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using System;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  public class SecurityCacheService : ISecurityCacheService
  {
    private IRemoteCacheService _remoteCacheService;

    private ILocalCacheService _localCacheService;

    private ApplicationConfigurationInfo _applicationConfigurationInfo;

    public SecurityCacheService(IRemoteCacheService remoteCacheService, ILocalCacheService localCacheService, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _remoteCacheService = remoteCacheService;
      _localCacheService = localCacheService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    public async Task<TValue> GetValueAsync<TValue>(string key)
    {
      if(_applicationConfigurationInfo.RedisCacheSettings.IsEnabled)
      {
        return await _remoteCacheService.GetValueAsync<TValue>(key);
      }
      else
      {
        return _localCacheService.GetValue<TValue>(key);
      }
    }

    public async Task RemoveAsync(params string[] keys)
    {
      if (_applicationConfigurationInfo.RedisCacheSettings.IsEnabled)
      {
        await _remoteCacheService.RemoveAsync(keys);
      }
      else
      {
        _localCacheService.Remove(keys);
      }
    }

    public async Task SetValueAsync<TValue>(string key, TValue value, TimeSpan expiration)
    {
      if (_applicationConfigurationInfo.RedisCacheSettings.IsEnabled)
      {
        await _remoteCacheService.SetValueAsync(key, value, expiration);
      }
      else
      {
        _localCacheService.SetValue(key, value, expiration);
      }
    }
  }
}
