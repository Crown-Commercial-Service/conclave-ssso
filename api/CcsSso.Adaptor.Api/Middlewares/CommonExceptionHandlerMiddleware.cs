using CcsSso.Shared.Domain.Excecptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.Middlewares
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
      catch (ResourceNotFoundException ex)
      {
        await HandleException(context, string.Empty, ex, HttpStatusCode.NotFound);
      }
      catch (ResourceAlreadyExistsException ex)
      {
        await HandleException(context, string.Empty, ex, HttpStatusCode.Conflict);
      }
      catch (DbUpdateConcurrencyException ex)
      {
        await HandleException(context, string.Empty, ex, HttpStatusCode.Conflict);
      }
      catch (CcsSsoException ex)
      {
        await HandleException(context, ex.Message, ex, HttpStatusCode.BadRequest);
      }
      catch (Exception ex)
      {
#if DEBUG
        await HandleException(context, ex.Message, ex, HttpStatusCode.InternalServerError);
# else
        await HandleException(context, "ERROR", ex, HttpStatusCode.InternalServerError);
#endif
      }
    }

    private async Task HandleException(HttpContext context, string displayError, Exception ex, HttpStatusCode statusCode)
    {
      Console.WriteLine(ex.Message);
      Console.WriteLine(JsonConvert.SerializeObject(ex));
      context.Response.StatusCode = (int)statusCode;
      await context.Response.WriteAsync(displayError);
    }
  }
}
