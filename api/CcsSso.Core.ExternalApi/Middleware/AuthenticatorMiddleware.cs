using CcsSso.Domain;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace CcsSso.ExternalApi.Middleware
{
  public class AuthenticatorMiddleware
  {

    private RequestDelegate _next;
    private readonly AppSetting _appSetting;

    public AuthenticatorMiddleware(RequestDelegate next, AppSetting appSetting)
    {
      _next = next;
      _appSetting = appSetting;
    }

    public async Task Invoke(HttpContext context)
    {
      var apiKey = context.Request.Headers["X-API-Key"];

      if (string.IsNullOrEmpty(apiKey) || apiKey != _appSetting.ApiKey)
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
