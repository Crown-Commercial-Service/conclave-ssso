using CcsSso.Adaptor.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperApiService
  {
    Task<T> GetAsync<T>(WrapperApi wrapperApi, string? url, string cacheKey, string errorMessage, bool cacheEnabledForRequest = true);

    Task<T> PostAsync<T>(WrapperApi wrapperApi, string? url, object requestData, string errorMessage);

    Task PutAsync(WrapperApi wrapperApi, string? url, object requestData, string errorMessage);
  }
}
