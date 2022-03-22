using CcsSso.Security.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
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
        await HandleException(context, string.Empty, ex, HttpStatusCode.Unauthorized);
      }
      catch (RecordNotFoundException ex)
      {
        await HandleException(context, string.Empty, ex, HttpStatusCode.NotFound);
      }
      catch (CcsSsoException ex)
      {
        await HandleException(context, ex.Message, ex, HttpStatusCode.BadRequest);
      }
      catch (SecurityException ex)
      {
        await HandleException(context, "SECURITY_ERROR", ex, HttpStatusCode.BadRequest);
      }
      catch (Exception ex)
      {
#if DEBUG
        await HandleException(context, ex.Message, ex, HttpStatusCode.InternalServerError);
#else
        await HandleException(context, "SECURITY_API_ERROR", ex, HttpStatusCode.InternalServerError);
#endif
      }
    }

    private async Task HandleException(HttpContext context, string displayError, Exception ex, HttpStatusCode statusCode)
    {
      Console.WriteLine(ex.Message);
      Console.WriteLine(JsonConvert.SerializeObject(ex));
      context.Response.StatusCode = (int)statusCode;

      if (displayError == "SECURITY_ERROR")
      {
        var errorInfo = JsonConvert.DeserializeObject<ErrorInfo>(ex.Message);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(errorInfo);
      }
      else
      {
        await context.Response.WriteAsync(displayError);
      }

    }
  }
}
