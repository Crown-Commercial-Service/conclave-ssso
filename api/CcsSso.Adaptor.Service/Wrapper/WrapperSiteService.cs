using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperSiteService : IWrapperSiteService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperSiteService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<WrapperOrganisationSiteInfoList> GetOrganisationSitesAsync(string organisationId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationSiteInfoList>(WrapperApi.Organisation, $"{organisationId}/sites", $"{CacheKeyConstant.OrgSites}-{organisationId}",
        "ERROR_RETRIEVING_ORGANISATION_SITES");
      return result;
    }

    public async Task<WrapperOrganisationSiteResponse> GetOrganisationSiteAsync(string organisationId, int siteId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationSiteResponse>(WrapperApi.Organisation, $"{organisationId}/sites/{siteId}",
        $"{CacheKeyConstant.Site}-{organisationId}-{siteId}", "ERROR_RETRIEVING_ORGANISATION_SITE");
      return result;
    }
  }
}

