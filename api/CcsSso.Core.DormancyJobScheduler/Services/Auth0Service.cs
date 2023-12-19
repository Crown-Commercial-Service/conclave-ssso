using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Domain.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Core.DormancyJobScheduler.Services
{
  public class Auth0Service : IAuth0Service
  {

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DormancyAppSettings _settings;
    public Auth0Service(IHttpClientFactory httpClientFactory, DormancyAppSettings settings)
    {
      _httpClientFactory = httpClientFactory;
      _settings = settings;
    }

    public async Task<UserDataList> GetUsersDataAsync(string q, int page, int perPage)
    {
      try
      {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_settings.SecurityApiSettings.Url);
        client.DefaultRequestHeaders.Add("X-API-Key", _settings.SecurityApiSettings.ApiKey);
        var url = "security/data/user-search?q=" + q + "&page="+page+"&page-size="+perPage;
        var response = await client.GetAsync(url);
        var responseString = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
          var result = JsonConvert.DeserializeObject<UserDataList>(responseString);
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
          throw new CcsSsoException(responseString);
        }
      }
      catch (Exception e)
      {
        throw new CcsSsoException("USER_NOT_FOUND" + e.Message);
      }
    }

    public async Task UpdateUserStatusAsync(string userName, int status)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_settings.SecurityApiSettings.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _settings.SecurityApiSettings.ApiKey);

      var response = await client.PutAsync($"security/users/status?email={HttpUtility.UrlEncode(userName)}&status={status}", null);

      if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException("ERROR_USER_STATUS_UPDATE");
      }
    }
  }
}
