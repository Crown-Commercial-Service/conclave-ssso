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
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Middleware
{
  public class AuthenticationMiddleware
  {
    private RequestDelegate _next;
    private readonly ITokenService _tokenService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IRemoteCacheService _remoteCacheService;
    private List<string> allowedPaths = new List<string>()
    {
      "auth/backchannel_logout", "auth/get_refresh_token","auth/send_reset_mfa_notification","auth/reset_mfa_by_ticket",
      "organisation/register", "user/useractivationemail", "user/permissions"
    };

    private List<string> allowedPathsForXSRFValidation = new List<string>()
    {
      "auth/create_session"
    };

    private const string allowedCiiRoute = "cii";
    private List<string> restrictedCiiPaths = new List<string>()
    {
      "cii/delete-scheme",  "cii/add-scheme"
    };

    public AuthenticationMiddleware(RequestDelegate next, ITokenService tokenService,
      ApplicationConfigurationInfo applicationConfigurationInfo, IRemoteCacheService remoteCacheService)
    {
      _next = next;
      _tokenService = tokenService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _remoteCacheService = remoteCacheService;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext)
    {
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      requestContext.IpAddress = context.GetRemoteIPAddress();
      requestContext.Device = context.Request.Headers["User-Agent"];

      if (allowedPaths.Contains(path) || (path.Contains(allowedCiiRoute) && !restrictedCiiPaths.Any(rp => rp == path)))
      {
        await _next(context);
        return;
      }

      if (context.Request.Headers.ContainsKey("Authorization"))
      {

        if (!string.IsNullOrEmpty(_applicationConfigurationInfo.CustomDomain) && !allowedPathsForXSRFValidation.Contains(path))
        {
          var cookie = context.Request.Cookies["XSRF-TOKEN-SVR"];
          var header = context.Request.Headers["x-xsrf-token"];
          if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(header) || cookie != header)
          {
            throw new UnauthorizedAccessException();
          }
        }

        var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(bearerToken))
        {
          var token = bearerToken.Split(' ').Last();
          var result = await _tokenService.ValidateTokenAsync(token, _applicationConfigurationInfo.JwtTokenValidationInfo.JwksUrl,
            _applicationConfigurationInfo.JwtTokenValidationInfo.IdamClienId, _applicationConfigurationInfo.JwtTokenValidationInfo.Issuer,
            new List<string>() { "uid", "ciiOrgId", "sub", JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Exp, "roles" });

          if (result.IsValid)
          {
            var userId = result.ClaimValues["uid"];
            var ciiOrgId = result.ClaimValues["ciiOrgId"];
            var sub = result.ClaimValues["sub"];
            var jti = result.ClaimValues[JwtRegisteredClaimNames.Jti];
            long.TryParse(result.ClaimValues[JwtRegisteredClaimNames.Exp], out long exp);

            if (path == "auth/sign_out")
            {
              await _remoteCacheService.SetValueAsync(CacheKeyConstant.BlockedListKey + jti, sub, new TimeSpan(exp));
            }
            else
            {
              var forceSignout = await _remoteCacheService.GetValueAsync<bool>(CacheKeyConstant.ForceSignoutKey + sub);
              //check if user is entitled to force signout
              if (forceSignout)
              {
                if (path == "auth/create_session")
                {
                  await _remoteCacheService.RemoveAsync(CacheKeyConstant.ForceSignoutKey + sub);
                }
                else
                {
                  throw new UnauthorizedAccessException();
                }
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
            }

            requestContext.UserId = int.Parse(userId);
            requestContext.CiiOrganisationId = ciiOrgId;
            requestContext.UserName = sub;
            requestContext.Roles = result.ClaimValues["roles"].Split(",").ToList();
            await _next(context);
          }
          else if (path == "auth/sign_out")
          {
            await _next(context);
          }
          else
          {
            throw new UnauthorizedAccessException();
          }
        }
      }
      else if (path == "auth/sign_out")
      {
        await _next(context);
      }
      else
      {
        throw new UnauthorizedAccessException();
      }
    }
  }
}
