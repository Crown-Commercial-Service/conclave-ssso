using CcsSso.Core.Domain.Contracts.External;
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
    // #Delegated
    private List<string> allowedPaths = new List<string>()
    {
      "users/delegate-user-validation", "user-profiles/delegate-user-validation"
    };

    public AuthenticatorMiddleware(RequestDelegate next, ApplicationConfigurationInfo appConfig, ITokenService tokenService, IRemoteCacheService remoteCacheService)
    {
      _next = next;
      _appConfig = appConfig;
      _tokenService = tokenService;
      _remoteCacheService = remoteCacheService;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext, IConfigurationDetailService configurationDetailService)
    {
      var apiKey = context.Request.Headers["X-API-Key"];
      var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      requestContext.IpAddress = context.GetRemoteIPAddress();
      requestContext.Device = context.Request.Headers["User-Agent"];
      requestContext.apiKey = apiKey;

      if (allowedPaths.Contains(path))
      {
        await _next(context);
        return;
      }

      // #Deleated: To identify delegate user search
      var isDelegated = context.Request.Query["is-delegated"].FirstOrDefault();
      requestContext.IsDelegated = isDelegated != null ? Convert.ToBoolean(isDelegated) : false;

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
            new List<string>() { "uid", "ciiOrgId", "sub", JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Exp, "roles", "caller", "sid" });

          if (result.IsValid)
          {
            var sub = result.ClaimValues["sub"];
            var jti = result.ClaimValues[JwtRegisteredClaimNames.Jti];
            var sessionId = result.ClaimValues["sid"];

            var isInvalidSession = await _remoteCacheService.GetValueAsync<bool>(sessionId);
            Console.WriteLine($"SessionId : {sessionId} **==** invalid: {isInvalidSession}");
            if (isInvalidSession) //if session was invalidated due to logout from other clients
            {
              throw new UnauthorizedAccessException();
            }

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


            if (result.ClaimValues["caller"] == "service")
            {
              requestContext.ServiceClientId = result.ClaimValues["sub"];
              requestContext.ServiceId = int.Parse(result.ClaimValues["uid"]);
            }
            else
            {
              requestContext.UserId = int.Parse(result.ClaimValues["uid"]);
              requestContext.UserName = result.ClaimValues["sub"];

              var serviceId = await configurationDetailService.GetDashboardServiceIdAsync();
              requestContext.ServiceId = serviceId;
            }

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
