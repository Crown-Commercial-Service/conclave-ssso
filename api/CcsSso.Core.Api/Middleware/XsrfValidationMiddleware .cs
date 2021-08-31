using CcsSso.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Middleware
{
  public class XsrfValidationMiddleware
  {
    private RequestDelegate _next;

    private List<string> allowedPaths = new List<string>()
    {
      "auth/create_session"
    };
    public XsrfValidationMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      if (!allowedPaths.Contains(path))
      {
        var cookie = context.Request.Cookies["XSRF-TOKEN-SVR"];
        var header = context.Request.Headers["x-xsrf-token"];
        if(cookie != header)
        {
          throw new ForbiddenException();
        }
      }
      await _next(context);
    }
  }
}
