using CcsSso.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace CcsSso.Api.Middleware
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
        await HandleException(context, ex.ToString(), ex, HttpStatusCode.Unauthorized);
      }
      catch (ResourceNotFoundException ex)
      {
        await HandleException(context, ex.ToString(), ex, HttpStatusCode.NotFound);
      }
      catch (DbUpdateConcurrencyException ex)
      {
        await HandleException(context, ex.ToString(), ex, HttpStatusCode.Conflict);
      }
      catch(MethodNotAllowedException ex)
      {
        await HandleException(context, ex.ToString(), ex, HttpStatusCode.MethodNotAllowed);
      }
      catch (ForbiddenException ex)
      {
        await HandleException(context, ex.ToString(), ex, HttpStatusCode.Forbidden);
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
      context.Response.StatusCode = (int)statusCode;
      await context.Response.WriteAsync(displayError);
    }
  }
}
