using CcsSso.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Middleware
{
  public class RequestLogMiddleware
  {
    private RequestDelegate _next;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;

    public RequestLogMiddleware(RequestDelegate next, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _next = next;
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    public async Task Invoke(HttpContext context)
    {
      var fullPath = context.Request.Path.Value;
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      if (path != "swagger/index.html")
      {
        var method = context.Request.Method;
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
        var token = bearerToken?.Split(' ').Last();

        Console.WriteLine($"EXTERNAL-API-LOGS:- Token: {token}, ApiKey: {apiKey}, Method: {method}, FullPath: {fullPath}");

        using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
          var bodyString = await reader.ReadToEndAsync();

          Console.WriteLine($"EXTERNAL-API-LOGS:- RequestBody: {bodyString}");

          // Reset the request body stream position so the next middleware can read it
          context.Request.Body.Position = 0;
        }
      }
      await _next(context);
    }
  }
}

