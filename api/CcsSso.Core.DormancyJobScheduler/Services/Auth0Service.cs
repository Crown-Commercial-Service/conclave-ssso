using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Helper;
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
    private readonly Auth0TokenHelper _tokenHelper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DormancyAppSettings _settings;
    public Auth0Service(Auth0TokenHelper tokenHelper, IHttpClientFactory httpClientFactory, DormancyAppSettings settings)
    {
      _tokenHelper = tokenHelper;
      _httpClientFactory = httpClientFactory;
      _settings = settings;
    }

    public async Task<UserListDetails> GetUsersByLastLogin(string fromDate, string toDate, int page, int perPage)
    {
      try
      {
        var managementApiToken = await _tokenHelper.GetAuth0ManagementApiTokenAsync();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("authorization", "Bearer " + managementApiToken);
        string query = string.Empty;
        query = HttpUtility.UrlEncode($"last_login:[{fromDate} TO {toDate}]");
        
        var response = await client.GetAsync(_settings.Auth0ConfigurationInfo.ManagementApiBaseUrl + $"/api/v2/users?q={query}&page={page}&per_page={perPage}&include_totals=true&search_engine=v3");
        var responseString = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
          var result = JsonConvert.DeserializeObject<UserListDetails>(responseString);
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
        throw new CcsSsoException("USER_NOT_FOUND");
      }
    }
  }
}
