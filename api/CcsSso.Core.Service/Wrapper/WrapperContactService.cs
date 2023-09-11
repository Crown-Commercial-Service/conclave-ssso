using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.Wrapper
{
  public class WrapperContactService : IWrapperContactService
  {
    private readonly IWrapperApiService _wrapperApiService;

    public WrapperContactService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    #region OrganisationContact
    public async Task<bool> DeleteOrganisationContactAsync(string organisationId, int contactId)
    {
     return await _wrapperApiService.DeleteAsync<bool>(WrapperApi.Contact, $"organisations/{organisationId}/contacts/{contactId}", "ERROR_DELETING_ORGANISATION_CONTACT");
    }

    public async Task<OrganisationContactInfoList> GetOrganisationContactListAsync(string organisationId)
    {
      return await _wrapperApiService.GetAsync<OrganisationContactInfoList>(WrapperApi.Contact, $"organisations/{organisationId}/contacts", "", "ERROR_GETTING_ORGANISATION_CONTACTS");
    }

    #endregion
  }
}
