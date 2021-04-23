using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CcsSso.ExternalApi.Middleware
{
  public class AuthenticatorMiddleware
  {

    private RequestDelegate _next;
    private readonly ApplicationConfigurationInfo _appConfig;
    private readonly ITokenService _tokenService;

    public AuthenticatorMiddleware(RequestDelegate next, ApplicationConfigurationInfo appConfig, ITokenService tokenService)
    {
      _next = next;
      _appConfig = appConfig;
      _tokenService = tokenService;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext)
    {
      var apiKey = context.Request.Headers["X-API-Key"];
      var authHeader = context.Request.Headers["Authorize"].ToString();
      var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();

      if (string.IsNullOrWhiteSpace(bearerToken) && (string.IsNullOrEmpty(apiKey) || apiKey != _appConfig.ApiKey))
      {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
      }
      else
      {
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
          var token = bearerToken.Split(' ').Last();
          var result = await _tokenService.ValidateTokenAsync(token, _appConfig.JwtTokenValidationInfo.JwksUrl,
            _appConfig.JwtTokenValidationInfo.IdamClienId, _appConfig.JwtTokenValidationInfo.Issuer, new List<string>() { "uid", "ciiOrgId" });

          if (result.IsValid)
          {
            var userId = result.ClaimValues["uid"];
            var ciiOrgId = result.ClaimValues["ciiOrgId"];
            requestContext.UserId = int.Parse(userId);
            requestContext.CiiOrganisationId = ciiOrgId;
          }
          else
          {
            throw new UnauthorizedAccessException();
          }
        }
        await _next(context);
      }
    }
  }
}
