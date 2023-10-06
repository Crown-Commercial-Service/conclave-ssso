using CcsSso.Core.DataMigrationJobScheduler.Constants;
using CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace CcsSso.Core.DataMigrationJobScheduler.Wrapper
{
  public class WrapperApiService : IWrapperApiService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    public WrapperApiService(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    public async Task<T> GetAsync<T>(WrapperApi wrapperApi, string? url, string errorMessage, bool cacheEnabledForRequest = true)
    {
      

      var client = GetHttpClient(wrapperApi);

      var response = await client.GetAsync(url);
      var responseString = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
      {
        var result = JsonConvert.DeserializeObject<T>(responseString);
       
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new Exception("Resource not found");
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new  Exception(responseString);
      }
      else
      {
        throw new Exception(errorMessage);
      }
    }

    public async Task<T> PostAsync<T>(WrapperApi wrapperApi, string? url, object requestData, string errorMessage)
    {
      var client = GetHttpClient(wrapperApi);

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
        throw new Exception("Resource not found");
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new Exception(responseString);
      }
      else if (response.StatusCode == HttpStatusCode.Conflict)
      {
        throw new Exception("Resource Already Exists");
      }
      else
      {
        throw new Exception(errorMessage);
      }
    }
    public async Task<bool> DeleteAsync(WrapperApi wrapperApi, string? url, string errorMessage)
    {
      var client = GetHttpClient(wrapperApi);

      var response = await client.DeleteAsync(url);
      var responseString = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
      {
        return true;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new Exception("Resource not found");
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new Exception(responseString);
      }
      else
      {
        throw new Exception(errorMessage);
      }
    }

    public async Task PutAsync(WrapperApi wrapperApi, string? url, object requestData, string errorMessage)
    {
      var client = GetHttpClient(wrapperApi);

      HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var response = await client.PutAsync(url, data);
      var responseString = await response.Content.ReadAsStringAsync();
      if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new Exception(responseString);
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new Exception("Resource not found");
      }
      else if (response.StatusCode == HttpStatusCode.Conflict)
      {
        throw new Exception("Resource Already Exists");
      }
      else if (response.StatusCode == HttpStatusCode.Conflict)
      {
        throw new DbUpdateConcurrencyException();
      }
      else if (!response.IsSuccessStatusCode)
      {
        throw new Exception(errorMessage);
      }
    }
    public async Task<T> PutAsync<T>(WrapperApi wrapperApi, string? url, object requestData, string errorMessage)
    {
      var client = GetHttpClient(wrapperApi);

      HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var response = await client.PutAsync(url, data);
      var responseString = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        var result = JsonConvert.DeserializeObject<T>(responseString);
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new Exception(responseString);
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new Exception("Resource not found");
      }
      else if (!response.IsSuccessStatusCode)
      {
        throw new Exception(errorMessage);
      }
      else
      {
        throw new Exception(errorMessage);
      }
    }
    public async Task<T> DeleteAsync<T>(WrapperApi wrapperApi, string? url, string errorMessage)
    {
      var client = GetHttpClient(wrapperApi);

      var response = await client.DeleteAsync(url);
      var responseString = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
      {
        var result = JsonConvert.DeserializeObject<T>(responseString);
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new Exception("Resource not found");
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new Exception(responseString);
      }
      else
      {
        throw new Exception(errorMessage);
      }
    }

    private HttpClient GetHttpClient(WrapperApi wrapperApi)
    {
      var clientName = wrapperApi switch
      {
        WrapperApi.Organisation => "OrgWrapperApi",        
        _ => "UserWrapperApi"
      };
      var client = _httpClientFactory.CreateClient(clientName);
      client.Timeout = new TimeSpan(1, 0, 0);
      return client;
    }

  }
}
