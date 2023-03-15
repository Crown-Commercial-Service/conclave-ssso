using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface INotificationApiService
  {
    Task<T> PostAsync<T>(string? url, object requestData, string errorMessage);
  }
}
