using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.Middlewares
{
  public class AuthenticationMiddleware
  {
    private RequestDelegate _next;
    private AppSetting _appSetting;

    public AuthenticationMiddleware(RequestDelegate next, AppSetting appSetting)
    {
      _next = next;
      _appSetting = appSetting;
    }

    public async Task Invoke(HttpContext context, AdaptorRequestContext requestContext, IConsumerService consumerService)
    {
      var consumerClientId = context.Request.Headers["X-Consumer-ClientId"];
      var apiKey = context.Request.Headers["X-API-Key"];

      if (string.IsNullOrWhiteSpace(consumerClientId) || string.IsNullOrWhiteSpace(apiKey) || apiKey != _appSetting.ApiKey)
      {
        Console.WriteLine($"Vijay-Middleware-Invoke-Adaptor-ClientId null or empty-consumerClientId {consumerClientId}");

        throw new UnauthorizedAccessException();
      }
      else
      {
        var consumer = await consumerService.GetConsumerByClientId(consumerClientId);
        if (consumer == null)
        {
          Console.WriteLine($"Vijay-Middleware-Invoke-Adaptor-consumerClientId {consumerClientId}");
          throw new UnauthorizedAccessException();
        }
        else
        {
          requestContext.ConsumerId = consumer.Id;
          await _next(context);
        }
      }
    }
  }
}
