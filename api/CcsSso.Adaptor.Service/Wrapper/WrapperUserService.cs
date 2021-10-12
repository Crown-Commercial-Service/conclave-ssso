using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperUserService : IWrapperUserService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperUserService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<WrapperUserResponse> GetUserAsync(string userName)
    {
      var result = await _wrapperApiService.GetAsync<WrapperUserResponse>(WrapperApi.User, $"?user-id={HttpUtility.UrlEncode(userName)}", $"{CacheKeyConstant.User}-{userName}", "ERROR_RETRIEVING_USER");
      return result;
    }

    public async Task<string> UpdateUserAsync(string userName, WrapperUserRequest wrapperUserRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.User, $"?user-id={userName}", wrapperUserRequest, "ERROR_UPDATING_USER");
      return userName;
    }
  }
}
