using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.Wrapper
{
    public class WrapperSecurityService : IWrapperSecurityService
  {
    private readonly IWrapperApiService _wrapperApiService;
    public WrapperSecurityService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }
    public async Task<IdamUser> GetUserByEmail(string email)
    {
      var result = await _wrapperApiService.GetAsync<IdamUser>(WrapperApi.Security, $"/security/users?email={email}", $"{CacheKeyConstant.Security}-{email}", "ERROR_RETRIEVING_IDAM_USER");
      return result;
    }
  }
}
