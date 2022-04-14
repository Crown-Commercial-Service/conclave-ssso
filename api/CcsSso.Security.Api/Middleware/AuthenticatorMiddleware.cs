using CcsSso.Security.Domain.Dtos;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Middleware
{
  public class AuthenticatorMiddleware
  {
    private RequestDelegate _next;
    private readonly ApplicationConfigurationInfo _appSetting;
    private readonly ITokenService _tokenService;
    private List<string> allowedPaths = new List<string>()
    {
      "security/nominate", "security/.well-known/openid-configuration", ".well-known/openid-configuration"
    };

    private List<string> urlparamPaths = new List<string>()
    {
      "security/samlp"
    };

    public AuthenticatorMiddleware(RequestDelegate next, ApplicationConfigurationInfo appSetting, ITokenService tokenService)
    {
      _next = next;
      _appSetting = appSetting;
      _tokenService = tokenService;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext)
    {
      var apiKey = context.Request.Headers["X-API-Key"];
      var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');

      if (allowedPaths.Contains(path) || urlparamPaths.Any(p => path.StartsWith(p)))
      {
        await _next(context);
        return;
      }

      if (!_appSetting.SecurityApiKeySettings.ApiKeyValidationExcludedRoutes.Contains(path) && string.IsNullOrWhiteSpace(bearerToken) &&
        ((string.IsNullOrEmpty(apiKey) || apiKey != _appSetting.SecurityApiKeySettings.SecurityApiKey)))
      {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
      }
      else
      {
        if (!string.IsNullOrEmpty(apiKey))
        {
          await _next(context);
        }
        else if (_appSetting.SecurityApiKeySettings.BearerTokenValidationIncludedRoutes.Contains(path) && !string.IsNullOrWhiteSpace(bearerToken))
        {
          var token = bearerToken.Split(' ').Last();
          Console.WriteLine($"Token In Header:- {token}");
          var result = await _tokenService.ValidateTokenWithoutAudienceAsync(token, _appSetting.JwtTokenConfiguration.JwksUrl,
            _appSetting.JwtTokenConfiguration.IdamClienId, _appSetting.JwtTokenConfiguration.Issuer,
            new List<string>() { "uid", "ciiOrgId", "sub", JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Exp, "roles", "caller" });

          if (result.IsValid)
          {
            var sub = result.ClaimValues["sub"];
            var jti = result.ClaimValues[JwtRegisteredClaimNames.Jti];


            if (result.ClaimValues["caller"] == "service")
            {
              requestContext.ServiceClientId = result.ClaimValues["sub"];
              requestContext.ServiceId = int.Parse(result.ClaimValues["uid"]);
            }
            else
            {
              requestContext.UserId = int.Parse(result.ClaimValues["uid"]);
              requestContext.UserName = result.ClaimValues["sub"];
            }

            requestContext.CiiOrganisationId = result.ClaimValues["ciiOrgId"];
            requestContext.Roles = result.ClaimValues["roles"].Split(",").ToList();
            await _next(context);
          }
          else
          {
            throw new UnauthorizedAccessException();
          }
        }
        else if (_appSetting.SecurityApiKeySettings.ApiKeyValidationExcludedRoutes.Contains(path))
        {
          // For ApiKeyValidationExcludedRoutes and bearer token not required (unauthenticated requests)
          await _next(context);
        }
        else
        {
          throw new UnauthorizedAccessException();
        }
      }
    }
  }
}
