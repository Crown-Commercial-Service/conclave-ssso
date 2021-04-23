using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class IdamService : IIdamService
  {
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    public IdamService(ApplicationConfigurationInfo applicationConfigurationInfo, IHttpClientFactory httpClientFactory)
    {
      _applicationConfigurationInfo = applicationConfigurationInfo;
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
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);

      var response = await client.DeleteAsync($"security/deleteuser?email={userName}");

      if (response.StatusCode == System.Net.HttpStatusCode.OK)
      {
      }
      else
      {
        throw new CcsSsoException("ERROR_IDAM_USER_DELETION_FAILED");
      }
    }

    /// <summary>
    /// Register user in IDAM
    /// </summary>
    /// <param name="securityApiUserInfo"></param>
    /// <returns></returns>
    public async Task RegisterUserInIdamAsync(SecurityApiUserInfo securityApiUserInfo)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);
      var byteContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(securityApiUserInfo)));
      byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var response = await client.PostAsync("security/register", byteContent);

      if (response.StatusCode == System.Net.HttpStatusCode.OK)
      {
      }
      else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        if (responseContent == "USERNAME_EXISTS")
        {
          throw new ResourceAlreadyExistsException();
        }
        else
        {
          throw new CcsSsoException("ERROR_IDAM_REGISTRATION_FAILED");
        }
      }
      else
      {
        throw new CcsSsoException("ERROR_IDAM_REGISTRATION_FAILED");
      }
    }
  }
}
