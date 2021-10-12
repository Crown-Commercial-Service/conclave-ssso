using CcsSso.Domain.Contracts;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class CacheInvalidateService : ICacheInvalidateService
  {
    private readonly IRemoteCacheService _remoteCacheService;
    public CacheInvalidateService(IRemoteCacheService remoteCacheService)
    {
      _remoteCacheService = remoteCacheService;
    }

    public async Task RemoveUserCacheValuesOnDeleteAsync(string userName, string organisationId, List<int> contactPointIds)
    {
      List<string> cacheKeys = new()
      {
        $"{CacheKeyConstant.User}-{userName}",
        $"{CacheKeyConstant.OrganisationUsers}-{organisationId}",
        $"{CacheKeyConstant.UserContactPoints}-{userName}",
        $"{CacheKeyConstant.UserOrganisation}-{userName}"
      };
      contactPointIds.ForEach((cpid) => cacheKeys.Add($"{CacheKeyConstant.UserContactPoint}-{userName}-{cpid}"));

      await _remoteCacheService.RemoveAsync(cacheKeys.ToArray());
    }

    public async Task RemoveOrganisationCacheValuesOnDeleteAsync(string ciiOrganisationId, List<int> contactPointIds, Dictionary<string, List<int>> siteContactPoints)
    {
      List<string> cacheKeys = new()
      {
        $"{CacheKeyConstant.Organisation}-{ciiOrganisationId}",
        $"{CacheKeyConstant.OrganisationUsers}-{ciiOrganisationId}",
        $"{CacheKeyConstant.OrganisationContactPoints}-{ciiOrganisationId}",
        $"{CacheKeyConstant.OrgSites}-{ciiOrganisationId}"
      };
      contactPointIds.ForEach((cpid) => cacheKeys.Add($"{CacheKeyConstant.OrganisationContactPoint}-{ciiOrganisationId}-{cpid}"));

      foreach(var site in siteContactPoints)
      {
        cacheKeys.Add($"{CacheKeyConstant.Site}-{ciiOrganisationId}-{site.Key}");
        cacheKeys.Add($"{CacheKeyConstant.SiteContactPoints}-{ciiOrganisationId}-{site.Key}");
        site.Value.ForEach((scpid) => cacheKeys.Add($"{CacheKeyConstant.SiteContactPoint}-{ciiOrganisationId}-{site.Key}-{scpid}"));
      }

      await _remoteCacheService.RemoveAsync(cacheKeys.ToArray());
    }
  }
}
