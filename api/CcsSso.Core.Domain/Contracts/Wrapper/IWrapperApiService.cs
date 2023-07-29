using CcsSso.Domain.Constants;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.Wrapper
{
    // #Auto validation
    public interface IWrapperApiService
    {
        Task<T> GetAsync<T>(WrapperApi wrapperApi, string url, string cacheKey, string errorMessage, bool cacheEnabledForRequest = true);

        Task<T> PostAsync<T>(WrapperApi wrapperApi, string url, object requestData, string errorMessage);

        Task PutAsync(WrapperApi wrapperApi, string url, object requestData, string errorMessage);

        Task<T> PutAsync<T>(WrapperApi wrapperApi, string url, object requestData, string errorMessage);

        Task<bool> DeleteAsync(WrapperApi wrapperApi, string url, string errorMessage);

        Task<T> DeleteAsync<T>(WrapperApi wrapperApi, string url, string errorMessage);

    }
}
