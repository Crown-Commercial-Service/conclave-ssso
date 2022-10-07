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
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Middleware
{
  public class AuthenticationMiddleware
  {
    private RequestDelegate _next;
    private readonly ITokenService _tokenService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IRemoteCacheService _remoteCacheService;
    private List<string> allowedPathsForXSRFValidation = new List<string>()
    {
      "auth/sessions"
    };


    public List<string> AllowedPaths
    {
      get
      {
        List<string> allowedPaths = new List<string>();
        allowedPaths.AddRange(AllowedAuthAPIPath());
        allowedPaths.AddRange(AllowedOrganisationAPIPath());
        allowedPaths.AddRange(AllowedUserAPIPath());
        allowedPaths.AddRange(AllowedConfigAPIPath());
        allowedPaths.AddRange(AllowedCIIAPIPath());

        return allowedPaths;

      }
    }

    private List<string>  AllowedAuthAPIPath()
    {
      List<string> allowedPaths = new List<string>();
      allowedPaths.Add("auth/backchannel-logout");
      allowedPaths.Add("auth/mfa-reset-by-tickets");
      allowedPaths.Add("auth/refresh-tokens");
      allowedPaths.Add("auth/mfa-reset-notifications");
      return allowedPaths;
    }
    private List<string> AllowedOrganisationAPIPath()
    {
      List<string> allowedPaths = new List<string>();
      allowedPaths.Add("organisations/registrations");
      allowedPaths.Add("organisations/orgs-by-name");
      allowedPaths.Add("organisations/org-admin-join-notification");
      return allowedPaths;
    }
    private List<string> AllowedUserAPIPath()
    {
      List<string> allowedPaths = new List<string>();
      allowedPaths.Add("users/nominees");
      allowedPaths.Add("users/activation-emails");
      return allowedPaths;
    }
    private List<string> AllowedConfigAPIPath()
    {
      List<string> allowedPaths = new List<string>();
      allowedPaths.Add("configurations/country-details");
      return allowedPaths;
    }
    
    private List<string> AllowedCIIAPIPath()
    {
      List<string> allowedPaths = new List<string>();
      allowedPaths.Add("cii/schemes");
      allowedPaths.Add("cii/identifiers");
      allowedPaths.Add("cii/organisation-details");
      return allowedPaths;
    }

   

    public AuthenticationMiddleware(RequestDelegate next, ITokenService tokenService,
      ApplicationConfigurationInfo applicationConfigurationInfo, IRemoteCacheService remoteCacheService)
    {
      _next = next;
      _tokenService = tokenService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _remoteCacheService = remoteCacheService;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext, IConfigurationDetailService configurationDetailService)
    {
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      requestContext.IpAddress = context.GetRemoteIPAddress();
      requestContext.Device = context.Request.Headers["User-Agent"];
      var serviceId = await configurationDetailService.GetDashboardServiceIdAsync();
      requestContext.ServiceId = serviceId;

      if (AllowedPaths.Contains(path))
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
          // This was added temporary and will be removed after fix the api gateway issue
          Console.WriteLine("XSRF-TOKEN-SVR=" + cookie);
          Console.WriteLine("x-xsrf-token=" + header);
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
            new List<string>() { "uid", "ciiOrgId", "sub", JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Exp, "roles", "caller", "sid" });

          if (result.IsValid)
          {
            var userId = result.ClaimValues["uid"];
            var ciiOrgId = result.ClaimValues["ciiOrgId"];
            var sub = result.ClaimValues["sub"];
            var jti = result.ClaimValues[JwtRegisteredClaimNames.Jti];
            long.TryParse(result.ClaimValues[JwtRegisteredClaimNames.Exp], out long exp);

            if (path == "auth/sign-out")
            {
              await _remoteCacheService.SetValueAsync(CacheKeyConstant.BlockedListKey + jti, sub, new TimeSpan(exp));
            }
            else
            {
              var sessionId = result.ClaimValues["sid"];
              var isInvalidSession = await _remoteCacheService.GetValueAsync<bool>(sessionId);
              var forceSignout = await _remoteCacheService.GetValueAsync<bool>(CacheKeyConstant.ForceSignoutKey + sub);
              
              //check if user is entitled to force signout or invalid session (due to logout from other service)
              if (isInvalidSession || forceSignout)
              {
                if (path == "auth/sessions")
                {
                  await _remoteCacheService.RemoveAsync(sessionId);
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
            requestContext.UserName = sub;
            requestContext.CiiOrganisationId = ciiOrgId;
            requestContext.Roles = result.ClaimValues["roles"].Split(",").ToList();
            await _next(context);
          }
          else if (path == "auth/sign-out")
          {
            await _next(context);
          }
          else
          {
            throw new UnauthorizedAccessException();
          }
        }
      }
      else if (path == "auth/sign-out")
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
