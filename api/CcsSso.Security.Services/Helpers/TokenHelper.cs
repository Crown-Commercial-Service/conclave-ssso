using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Services.Helpers
{
  public class TokenHelper
  {
    private readonly ApplicationConfigurationInfo _appConfigInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    private static string token;
    public TokenHelper(ApplicationConfigurationInfo appConfigInfo, IHttpClientFactory httpClientFactory)
    {
      _appConfigInfo = appConfigInfo;
      _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetAuth0ManagementApiTokenAsync()
    {
      if (string.IsNullOrEmpty(token) || IsExpired(token))
      {
        using var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_appConfigInfo.Auth0ConfigurationInfo.ManagementApiBaseUrl);
        Dictionary<string, string> requestData = new Dictionary<string, string>
          {
            { "grant_type", "client_credentials"},
            { "client_id", _appConfigInfo.Auth0ConfigurationInfo.ClientId},
            { "client_secret", _appConfigInfo.Auth0ConfigurationInfo.ClientSecret},
            { "audience", _appConfigInfo.Auth0ConfigurationInfo.ManagementApiIdentifier},
          };

        HttpContent codeContent = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");
        // HttpContent codeContent = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));

        await CallAuth0TokenApiAsync(client, codeContent);
      }
      return token;
    }

    private bool IsExpired(string token)
    {
      var handler = new JwtSecurityTokenHandler();
      var decodedToken = handler.ReadToken(token) as JwtSecurityToken;

      if (decodedToken.ValidTo >= DateTime.UtcNow.AddMinutes(5))
      {
        return false;
      }
      return true;
    }

    private async Task CallAuth0TokenApiAsync(HttpClient client, HttpContent codeContent)
    {
      try
      {
        var response = await client.PostAsync("oauth/token", codeContent);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
          var responseContent = await response.Content.ReadAsStringAsync();
          var resultPersonContent = JsonConvert.DeserializeObject<Auth0Tokencontent>(responseContent);
          token = resultPersonContent.AccessToken;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
          throw new UnauthorizedAccessException();
        }
        else
        {
          throw new CcsSsoException("ERROR_COMMUNICATING");
        }
      }
      catch (HttpRequestException)
      {
        throw new CcsSsoException("ERROR_COMMUNICATING");
      }
    }
  }
}
