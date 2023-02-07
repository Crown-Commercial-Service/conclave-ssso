using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  // #Auto validation
  public interface IWrapperApiService
  {
    Task<T> PostAsync<T>(string? url, object requestData, string errorMessage);
  }
}
