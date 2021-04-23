using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Middleware
{
  public class AuthenticationMiddleware
  {
    private RequestDelegate _next;
    private readonly ITokenService _tokenService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private List<string> allowedPaths = new List<string>()
    {
      "auth/backchannel_logout", "auth/sign_out", "auth/get_refresh_token","auth/save_refresh_token", "cii", "cii/scheme", "cii/GetSchemes", "cii/GetSchemes", "cii/GetOrg", "cii/GetOrgs", "cii/GetIdentifiers", "cii/DeleteOrg"
    };

    public AuthenticationMiddleware(RequestDelegate next, ITokenService tokenService, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _next = next;
      _tokenService = tokenService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    public async Task Invoke(HttpContext context)
    {
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');

      if (allowedPaths.Contains(path))
      {
        await _next(context);
        return;
      }

      if (context.Request.Headers.ContainsKey("Authorization"))
      {
        var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(bearerToken))
        {
          var token = bearerToken.Split(' ').Last();
          var result = await _tokenService.ValidateTokenAsync(token, _applicationConfigurationInfo.JwtTokenValidationInfo.JwksUrl,
            _applicationConfigurationInfo.JwtTokenValidationInfo.IdamClienId, _applicationConfigurationInfo.JwtTokenValidationInfo.Issuer);

          if (result.IsValid)
          {
            await _next(context);
          }
          else
          {
            throw new UnauthorizedAccessException();
          }
        }
      }
      else
      {
        throw new UnauthorizedAccessException();
      }
    }
  }
}
