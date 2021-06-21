using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperSiteContactService : IWrapperSiteContactService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperSiteContactService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<WrapperOrganisationSiteContactInfo> GetSiteContactPointAsync(string organisationId, int siteId, int contactPointId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationSiteContactInfo>(WrapperApi.Organisation, $"{organisationId}/sites/{siteId}/contacts/{contactPointId}",
        $"{CacheKeyConstant.SiteContactPoint}-{organisationId}-{siteId}-{contactPointId}", "ERROR_RETRIEVING_SITE_CONTACT_POINT");
      return result;
    }

    public async Task<WrapperOrganisationSiteContactInfoList> GetSiteContactPointsAsync(string organisationId, int siteId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationSiteContactInfoList>(WrapperApi.Organisation, $"{organisationId}/sites/{siteId}/contacts",
        $"{CacheKeyConstant.SiteContactPoints}-{organisationId}-{siteId}", "ERROR_RETRIEVING_SITE_CONTACT_POINTS");
      return result;
    }

    public async Task<int> CreateSiteContactPointAsync(string organisationId, int siteId, WrapperContactPointRequest wrapperContactPointRequest)
    {
      var result = await _wrapperApiService.PostAsync<int>(WrapperApi.Organisation, $"{organisationId}/sites/{siteId}/contacts", wrapperContactPointRequest,
        "ERROR_CREATING_SITE_CONTACT_POINT");
      return result;
    }

    public async Task UpdateSiteContactPointAsync(string organisationId, int siteId, int contactPointId, WrapperContactPointRequest wrapperContactPointRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.Organisation, $"{organisationId}/sites/{siteId}/contacts/{contactPointId}", wrapperContactPointRequest,
        "ERROR_UPDATING_SITE_CONTACT_POINT");
    }

  }
}
