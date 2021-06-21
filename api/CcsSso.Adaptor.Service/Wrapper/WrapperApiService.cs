using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Excecptions;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperApiService : IWrapperApiService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRemoteCacheService _remoteCacheService;
    private readonly AppSetting _appSetting;
    public WrapperApiService(IHttpClientFactory httpClientFactory, IRemoteCacheService remoteCacheService, AppSetting appSetting)
    {
      _httpClientFactory = httpClientFactory;
      _remoteCacheService = remoteCacheService;
      _appSetting = appSetting;
    }

    public async Task<T> GetAsync<T>(WrapperApi wrapperApi, string? url, string cacheKey, string errorMessage, bool cacheEnabledForRequest = true)
    {
      if (_appSetting.RedisCacheSettings.IsEnabled && cacheEnabledForRequest)
      {
        var result = await _remoteCacheService.GetValueAsync<T>(cacheKey);
        if (result != null)
        {
          return result;
        }
      }

      var client = GetHttpClient(wrapperApi);

      var response = await client.GetAsync(url);
      var responseString = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
      {
        var result = JsonConvert.DeserializeObject<T>(responseString);
        if (_appSetting.RedisCacheSettings.IsEnabled && cacheEnabledForRequest)
        {
          await _remoteCacheService.SetValueAsync<T>(cacheKey, result,
            new TimeSpan(0, _appSetting.RedisCacheSettings.CacheExpirationInMinutes, 0));
        }
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

    public async Task PutAsync(WrapperApi wrapperApi, string? url, object requestData, string errorMessage)
    {
      var client = GetHttpClient(wrapperApi);

      HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var response = await client.PutAsync(url, data);
      var responseString = await response.Content.ReadAsStringAsync();
      if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new CcsSsoException(responseString);
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException(errorMessage);
      }
    }

    private HttpClient GetHttpClient(WrapperApi wrapperApi)
    {
      var clientName = wrapperApi == WrapperApi.User ? "UserWrapperApi" : wrapperApi == WrapperApi.Organisation ? "OrgWrapperApi" : "ContactWrapperApi";
      var client = _httpClientFactory.CreateClient(clientName);
      return client;
    }

  }
  
}
