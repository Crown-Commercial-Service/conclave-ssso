using CcsSso.Security.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
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
      "security/nominate"
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

      if (allowedPaths.Contains(path))
      {
        await _next(context);
        return;
      }

      if (!_appSetting.SecurityApiKeySettings.ApiKeyValidationExcludedRoutes.Contains(path) && ((string.IsNullOrEmpty(apiKey) || apiKey != _appSetting.SecurityApiKeySettings.SecurityApiKey)))
      {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
      }
      else
      {
        await _next(context);
      }
    }
  }
}
