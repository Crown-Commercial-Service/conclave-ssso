using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Cii
{
  public interface ICiiApiService
  {
    Task<T> GetAsync<T>(string url, string errorMessage);
  }
}
