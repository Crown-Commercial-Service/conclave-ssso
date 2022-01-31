using CcsSso.Security.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Middleware
{
  public class AuthenticatorMiddleware
  {
    private RequestDelegate _next;
    private readonly ApplicationConfigurationInfo _appSetting;
    private List<string> allowedPaths = new List<string>()
    {
      "security/nominate", "security/.well-known/openid-configuration"
    };

    private List<string> urlparamPaths = new List<string>()
    {
      "security/samlp"
    };

    public AuthenticatorMiddleware(RequestDelegate next, ApplicationConfigurationInfo appSetting)
    {
      _next = next;
      _appSetting = appSetting;
    }

    public async Task Invoke(HttpContext context)
    {
      var apiKey = context.Request.Headers["X-API-Key"];
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      Console.WriteLine($"Path {path}");

      if (allowedPaths.Contains(path) || urlparamPaths.Any(p => path.StartsWith(p)))
      {
        Console.WriteLine($"Allowed Path");
        await _next(context);
        return;
      }

      if (!_appSetting.SecurityApiKeySettings.ApiKeyValidationExcludedRoutes.Contains(path) && ((string.IsNullOrEmpty(apiKey) || apiKey != _appSetting.SecurityApiKeySettings.SecurityApiKey)))
      {
        Console.WriteLine($"Not in Allowed Paths and no key");
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
      }
      else
      {
        await _next(context);
      }
    }
  }
}
