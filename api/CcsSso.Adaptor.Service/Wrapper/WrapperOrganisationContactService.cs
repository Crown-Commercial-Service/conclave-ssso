using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperOrganisationContactService : IWrapperOrganisationContactService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperOrganisationContactService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<WrapperOrganisationContactInfo> GetOrganisationContactPointAsync(string organisationId, int contactPointId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationContactInfo>(WrapperApi.Organisation, $"{organisationId}/contacts/{contactPointId}",
        $"{CacheKeyConstant.OrganisationContactPoint}-{organisationId}-{contactPointId}", "ERROR_RETRIEVING_ORGANISATION_CONTACT_POINT");
      return result;
    }

    public async Task<WrapperOrganisationContactInfoList> GetOrganisationContactsAsync(string organisationId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationContactInfoList>(WrapperApi.Organisation, $"{organisationId}/contacts",
        $"{CacheKeyConstant.OrganisationContactPoints}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION_CONTACT_POINTS");
      return result;
    }

    public async Task<int> CreateOrganisationContactPointAsync(string organisationId, WrapperContactPointRequest wrapperContactPointRequest)
    {
      var result = await _wrapperApiService.PostAsync<int>(WrapperApi.Organisation, $"{organisationId}/contacts", wrapperContactPointRequest,
        "ERROR_CREATING_ORGANISAION_CONTACT_POINT");
      return result;
    }

    public async Task UpdateOrganisationContactPointAsync(string organisationId, int contactPointId, WrapperContactPointRequest wrapperContactPointRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.Organisation, $"{organisationId}/contacts/{contactPointId}", wrapperContactPointRequest,
        "ERROR_UPDATING_ORGANISAION_CONTACT_POINT");
    }
  }
}
