using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class WrapperCacheService : IWrapperCacheService
  {

    private readonly ApplicationConfigurationInfo _appConfig;
    private readonly IRemoteCacheService _remoteCacheService;
    public WrapperCacheService(ApplicationConfigurationInfo appConfig, IRemoteCacheService remoteCacheService)
    {
      _appConfig = appConfig;
      _remoteCacheService = remoteCacheService;
    }

    public async Task RemoveCacheAsync(params string[] keys)
    {
      if (_appConfig.RedisCacheSettings.IsEnabled)
      {
        await _remoteCacheService.RemoveAsync(keys);
      }
    }
  }
}
