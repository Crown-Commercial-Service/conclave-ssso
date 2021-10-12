using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperUserContactService : IWrapperUserContactService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperUserContactService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<WrapperUserContactInfo> GetUserContactPointAsync(string userName, int contactPointId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperUserContactInfo>(WrapperApi.User, $"contacts/{contactPointId}?user-id={HttpUtility.UrlEncode(userName)}",
        $"{CacheKeyConstant.UserContactPoint}-{userName}-{contactPointId}", "ERROR_RETRIEVING_USER_CONTACT_POINT");
      return result;
    }

    public async Task<WrapperUserContactInfoList> GetUserContactPointsAsync(string userName)
    {
      var result = await _wrapperApiService.GetAsync<WrapperUserContactInfoList>(WrapperApi.User, $"contacts?user-id={HttpUtility.UrlEncode(userName)}",
        $"{CacheKeyConstant.UserContactPoints}-{userName}", "ERROR_RETRIEVING_USER_CONTACT_POINTS");
      return result;
    }

    public async Task<int> CreateUserContactPointAsync(string userName, WrapperContactPointRequest wrapperContactPointRequest)
    {
      var result = await _wrapperApiService.PostAsync<int>(WrapperApi.User, $"contacts/?user-id={HttpUtility.UrlEncode(userName)}", wrapperContactPointRequest,
        "ERROR_CREATING_USER_CONTACT_POINT");
      return result;
    }

    public async Task UpdateUserContactPointAsync(string userName, int contactPointId, WrapperContactPointRequest wrapperContactPointRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.User, $"contacts/{contactPointId}?user-id={HttpUtility.UrlEncode(userName)}", wrapperContactPointRequest,
        "ERROR_UPDATING_USER_CONTACT_POINT");
    }
  }
}

