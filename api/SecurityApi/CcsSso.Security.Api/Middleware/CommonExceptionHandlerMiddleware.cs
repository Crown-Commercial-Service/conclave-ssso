using CcsSso.Security.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Middleware
{
  public class CommonExceptionHandlerMiddleware
  {
    private RequestDelegate _next;

    public CommonExceptionHandlerMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
      try
      {
        await _next(context);
      }
      catch (UnauthorizedAccessException ex)
      {
        await HandleException(context, ex.Message, ex, HttpStatusCode.Unauthorized);
      }
      catch (AuthenticationException ex)
      {
        await HandleException(context, ex.Message, ex, HttpStatusCode.NotFound);
      }
      catch (CcsSsoException ex)
      {
        await HandleException(context, ex.Message, ex, HttpStatusCode.BadRequest);
      }
#if DEBUG
      catch (Exception ex)
      {
        await context.Response.WriteAsync(ex.ToString());
        throw;
      }
#endif
    }

    private async Task HandleException(HttpContext context, string displayError, Exception ex, HttpStatusCode statusCode)
    {
      context.Response.StatusCode = (int)statusCode;
      await context.Response.WriteAsync(displayError);
    }
  }
}
