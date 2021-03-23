using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Dtos;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class AuthService : IAuthService
  {
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    public AuthService(ApplicationConfigurationInfo applicationConfigurationInfo, IHttpClientFactory httpClientFactory)
    {
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> ValidateBackChannelLogoutTokenAsync(string backChanelLogoutToken)
    {
      var client = _httpClientFactory.CreateClient();
      client.BaseAddress = new Uri(_applicationConfigurationInfo.JwtTokenValidationInfo.JwksUrl);
      var result = await client.GetAsync(string.Empty);
      var jsonKeys = await result.Content.ReadAsStringAsync();
      var jwks = new JsonWebKeySet(jsonKeys);
      var jwk = jwks.Keys.First();
      var validationParameters = new TokenValidationParameters
      {
        IssuerSigningKey = jwk,
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ValidAudience = _applicationConfigurationInfo.JwtTokenValidationInfo.IdamClienId,
        ValidIssuer = _applicationConfigurationInfo.JwtTokenValidationInfo.Issuer        
      };

      var tokenHandler = new JwtSecurityTokenHandler();

      try
      {
        tokenHandler.ValidateToken(backChanelLogoutToken, validationParameters, out _);
      }
      catch (Exception)
      {
        return false;
      }
      return true;
    }
  }
}
