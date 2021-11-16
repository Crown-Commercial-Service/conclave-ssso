using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Core.JobScheduler.Services
{
  public class IdamSupportService : IIdamSupportService
  {
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    public IdamSupportService(AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
      _appSettings = appSettings;
      _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Delete user in IDAM
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task DeleteUserInIdamAsync(string userName)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);

      var response = await client.DeleteAsync($"security/users?email={HttpUtility.UrlEncode(userName)}");

      if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException("ERROR_IDAM_USER_DELETION_FAILED");
      }
    }
  }
}
