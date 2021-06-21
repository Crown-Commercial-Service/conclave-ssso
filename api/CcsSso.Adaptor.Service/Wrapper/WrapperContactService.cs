using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperContactService : IWrapperContactService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperContactService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<WrapperContactResponse> GetContactAsync(int contactId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperContactResponse>(WrapperApi.Contact, $"{contactId}", $"{CacheKeyConstant.Contact}-{contactId}", "ERROR_RETRIEVING_CONTACT");
      return result;
    }

    public async Task<int> CreateContactAsync(WrapperContactRequest wrapperContactRequest)
    {
      var result = await _wrapperApiService.PostAsync<int>(WrapperApi.Contact, null, wrapperContactRequest, "ERROR_CREATING_CONTACT");
      return result;
    }

    public async Task UpdateContactAsync(int contactId, WrapperContactRequest wrapperContactRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.Contact, $"{contactId}", wrapperContactRequest, "ERROR_UPDATING_CONTACT");
    }
  }
}
