using CcsSso.Adaptor.Domain.Contracts.Cii;
using CcsSso.Shared.Domain.Excecptions;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Cii
{
  public class CiiApiService : ICiiApiService
  {
    private readonly IHttpClientFactory _httpClientFactory;

    public CiiApiService(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    public async Task<T> GetAsync<T>(string url, string errorMessage)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");

      var response = await client.GetAsync(url);
      var responseString = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
      {
        var result = JsonConvert.DeserializeObject<T>(responseString);
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new CcsSsoException(responseString);
      }
      else
      {
        throw new CcsSsoException(errorMessage);
      }
    }
  }
}
