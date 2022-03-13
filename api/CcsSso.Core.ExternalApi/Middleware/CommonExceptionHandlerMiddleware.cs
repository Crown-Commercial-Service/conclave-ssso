using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace CcsSso.ExternalApi.Middleware
{
  public class CommonExceptionHandlerMiddleware
  {
    private RequestDelegate _next;
    private readonly ILogger<CommonExceptionHandlerMiddleware> _logger;
    public CommonExceptionHandlerMiddleware(RequestDelegate next, ILogger<CommonExceptionHandlerMiddleware> logger)
    {
      _logger = logger;
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
      catch (ForbiddenException ex)
      {
        await HandleException(context, string.Empty, ex, HttpStatusCode.Forbidden);
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
      _logger.LogError(ex, displayError);

      context.Response.StatusCode = (int)statusCode;
      await context.Response.WriteAsync(displayError);
    }
  }
}
