using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

      var response = await client.DeleteAsync($"security/users?email={HttpUtility.UrlEncode(userName)}");

      if (!response.IsSuccessStatusCode)
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
      var response = await client.PostAsync("security/users", byteContent);

      if (!response.IsSuccessStatusCode)
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        if (responseContent == "USERNAME_EXISTS")
        {
          throw new ResourceAlreadyExistsException();
        }
        else if (responseContent == "ERROR_PASSWORD_TOO_WEAK")
        {
          throw new CcsSsoException("ERROR_PASSWORD_TOO_WEAK");
        }
        else
        {
          throw new CcsSsoException("ERROR_IDAM_REGISTRATION_FAILED");
        }
      }
    }

    public async Task<string> GetActivationEmailVerificationLink(string email)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);

      var url = "security/users/activation-email-verification-link?email=" + HttpUtility.UrlEncode(email);
      var response = await client.GetAsync(url);

      if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException("ERROR_IDAM_REGISTRATION_FAILED");
      }

      return await response.Content.ReadAsStringAsync();
    }



    public async Task UpdateUserMfaInIdamAsync(SecurityApiUserInfo securityApiUserInfo)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);
      var byteContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(securityApiUserInfo)));
      byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var response = await client.PostAsync("security/users/mfa", byteContent);

      if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException("ERROR_USER_MFA_UPDATE");
      }
    }

    public async Task ResetUserPasswordAsync(string userName)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);

      var content = new { UserName = userName, ForceLogout = true };
      var byteContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content)));
      byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var response = await client.PostAsync($"security/password-reset-requests", byteContent);

      if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        throw new CcsSsoException("ERROR_INVALID_IDAM_USER");
      }
      else if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException("ERROR_IDAM_USER_PASSWORD_RESET");
      }
    }
  }
}
