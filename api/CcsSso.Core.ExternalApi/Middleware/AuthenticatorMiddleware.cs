using CcsSso.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Middleware
{
  public class AuthenticatorMiddleware
  {

    private RequestDelegate _next;
    private readonly ApplicationConfigurationInfo _appConfig;
    private readonly ITokenService _tokenService;
    private readonly IRemoteCacheService _remoteCacheService;

    public AuthenticatorMiddleware(RequestDelegate next, ApplicationConfigurationInfo appConfig, ITokenService tokenService, IRemoteCacheService remoteCacheService)
    {
      _next = next;
      _appConfig = appConfig;
      _tokenService = tokenService;
      _remoteCacheService = remoteCacheService;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext)
    {
      var apiKey = context.Request.Headers["X-API-Key"];
      var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
      requestContext.IpAddress = context.GetRemoteIPAddress();
      requestContext.Device = context.Request.Headers["User-Agent"];

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
            _appConfig.JwtTokenValidationInfo.IdamClienId, _appConfig.JwtTokenValidationInfo.Issuer,
            new List<string>() { "uid", "ciiOrgId", "sub", JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Exp, "roles" });

          if (result.IsValid)
          {
            var sub = result.ClaimValues["sub"];
            var jti = result.ClaimValues[JwtRegisteredClaimNames.Jti];

            var forceSignout = await _remoteCacheService.GetValueAsync<bool>(CacheKeyConstant.ForceSignoutKey + sub);
            if (forceSignout) //check if user is entitled to force signout
            {
              throw new UnauthorizedAccessException();
            }
            else
            {
              var value = await _remoteCacheService.GetValueAsync<string>(CacheKeyConstant.BlockedListKey + jti);
              if (!string.IsNullOrEmpty(value))
              {
                //Should terminate surving
                throw new UnauthorizedAccessException();
              }
            }

            requestContext.UserId = int.Parse(result.ClaimValues["uid"]);
            requestContext.UserName = result.ClaimValues["sub"];
            requestContext.CiiOrganisationId = result.ClaimValues["ciiOrgId"];
            requestContext.Roles = result.ClaimValues["roles"].Split(",").ToList();
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
