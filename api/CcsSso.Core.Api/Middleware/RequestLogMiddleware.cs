using CcsSso.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Middleware
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
        var xsrfCookie = context.Request.Cookies["XSRF-TOKEN-SVR"];
        var xsrfTHeader = context.Request.Headers["x-xsrf-token"];
        var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
        var token = bearerToken?.Split(' ').Last();

        Console.WriteLine($"CORE-API-LOGS:- Token: {token}, XsrfCookie: {xsrfCookie}, XsrfTHeader: {xsrfTHeader}, Method: {method}, FullPath: {fullPath}");


        var contentType = context.Request.ContentType;
        //Not login the bulk upload file content
        if (contentType != null && contentType.Contains("multipart/form-data"))
        {
          await _next(context);
          return;
        }

        using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
          var bodyString = await reader.ReadToEndAsync();

          Console.WriteLine($"CORE-API-LOGS:- RequestBody: {bodyString}");

          // Reset the request body stream position so the next middleware can read it
          context.Request.Body.Position = 0;
        }
      }
      await _next(context);
    }
  }
}
