using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Domain.Exceptions;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  // #Auto validation
  public class WrapperApiService : IWrapperApiService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    public WrapperApiService(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    public async Task<T> PostAsync<T>(string? url, object requestData, string errorMessage)
    {
      var client = _httpClientFactory.CreateClient("OrgWrapperApi");

      HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var response = await client.PostAsync(url, data);
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
