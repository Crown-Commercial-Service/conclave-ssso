using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class AuthService : IAuthService
  {
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthService(ApplicationConfigurationInfo applicationConfigurationInfo, ITokenService tokenService, IHttpClientFactory httpClientFactory)
    {
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _tokenService = tokenService;
      _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> ValidateBackChannelLogoutTokenAsync(string backChanelLogoutToken)
    {
      var result = await _tokenService.ValidateTokenAsync(backChanelLogoutToken, _applicationConfigurationInfo.JwtTokenValidationInfo.JwksUrl, _applicationConfigurationInfo.JwtTokenValidationInfo.IdamClienId, _applicationConfigurationInfo.JwtTokenValidationInfo.Issuer);
      return result.IsValid;
    }

    public async Task ChangePasswordAsync(ChangePasswordDto changePassword)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("X-API-Key", _applicationConfigurationInfo.SecurityApiDetails.ApiKey);
      client.BaseAddress = new Uri(_applicationConfigurationInfo.SecurityApiDetails.Url);
      var url = "/security/changepassword";

      Dictionary<string, string> requestData = new Dictionary<string, string>
          {
            { "userName", changePassword.UserName},
            { "newPassword", changePassword.NewPassword},
            { "oldPassword", changePassword.OldPassword},
          };

      HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var result = await client.PostAsync(url, data);
      if (result.StatusCode == HttpStatusCode.BadRequest)
      {
        var errorMessage = await result.Content.ReadAsStringAsync();
        throw new CcsSsoException(errorMessage);
      }      
    }
  }
}
