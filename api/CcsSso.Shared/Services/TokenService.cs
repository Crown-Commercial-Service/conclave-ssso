using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class TokenService : ITokenService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private JsonWebKey _jwk = null;

    public TokenService(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    public async Task<JwtTokenValidationInfo> ValidateTokenAsync(string token, string jwksUrl, string audience, string issuer, List<string> claims = null)
    {
      var result = new JwtTokenValidationInfo();

      if (_jwk == null)
      {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(jwksUrl);
        var keys = await client.GetAsync(string.Empty);
        var jsonKeys = await keys.Content.ReadAsStringAsync();
        var jwks = JsonConvert.DeserializeObject<JsonWebKeySet>(jsonKeys);
        _jwk = jwks.Keys.First();
      }

      var validationParameters = new TokenValidationParameters
      {
        IssuerSigningKey = _jwk,
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ValidAudience = audience,
        ValidIssuer = issuer,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
      };

      var tokenHandler = new JwtSecurityTokenHandler();

      try
      {
        tokenHandler.ValidateToken(token, validationParameters, out _);
        result.IsValid = true;
        var jwtSecurityToken = tokenHandler.ReadJwtToken(token);
        if (claims != null)
        {
          Dictionary<string, string> resolvedClaims = new Dictionary<string, string>();
          foreach (var claim in claims.Where(c => c != "roles"))
          {
            var claimValue = jwtSecurityToken.Claims.First(c => c.Type == claim).Value;
            resolvedClaims.Add(claim, claimValue);
          }
          if (claims.Contains("roles"))
          {
            var roleList = jwtSecurityToken.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
            resolvedClaims.Add("roles", string.Join(',', roleList));
          }
          result.ClaimValues = resolvedClaims;
        }
      }
      catch (Exception e)
      {
        result.IsValid = false;
      }
      return result;
    }

    public async Task<JwtTokenValidationInfo> ValidateTokenWithoutAudienceAsync(string token, string jwksUrl, string audience, string issuer, List<string> claims = null)
    {
      var result = new JwtTokenValidationInfo();

      if (_jwk == null)
      {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(jwksUrl);
        var keys = await client.GetAsync(string.Empty);
        var jsonKeys = await keys.Content.ReadAsStringAsync();
        var jwks = JsonConvert.DeserializeObject<JsonWebKeySet>(jsonKeys);
        _jwk = jwks.Keys.First();
      }

      var validationParameters = new TokenValidationParameters
      {
        IssuerSigningKey = _jwk,
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ValidAudience = audience,
        ValidIssuer = issuer,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
      };

      var tokenHandler = new JwtSecurityTokenHandler();

      try
      {
        //tokenHandler.ValidateToken(token, validationParameters, out _);
        result.IsValid = true;
        var jwtSecurityToken = tokenHandler.ReadJwtToken(token);
        if (claims != null)
        {
          Dictionary<string, string> resolvedClaims = new Dictionary<string, string>();
          foreach (var claim in claims.Where(c => c != "roles"))
          {
            var claimValue = jwtSecurityToken.Claims.First(c => c.Type == claim).Value;
            resolvedClaims.Add(claim, claimValue);
          }
          if (claims.Contains("roles"))
          {
            var roleList = jwtSecurityToken.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
            resolvedClaims.Add("roles", string.Join(',', roleList));
          }
          result.ClaimValues = resolvedClaims;
        }
      }
      catch (Exception e)
      {
        result.IsValid = false;
      }
      return result;
    }
  }
}
