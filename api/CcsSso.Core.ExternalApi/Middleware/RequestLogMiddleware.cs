using CcsSso.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
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
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      if (path != "swagger/index.html" && path != "swagger/v1/swagger.json")
      {
        var fullPath = context.Request.Path.Value;
        var queryString = context.Request.QueryString;
        var method = context.Request.Method;
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var bearerToken = context.Request.Headers["Authorization"].FirstOrDefault();
        var token = bearerToken?.Split(' ').Last();
        string requestBodyString;
        string responseBodyString;

        using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
          requestBodyString = await reader.ReadToEndAsync();
          // Reset the request body stream position so the next middleware can read it
          context.Request.Body.Position = 0;
        }

        var originalBody = context.Response.Body;
        using var newBody = new MemoryStream();
        context.Response.Body = newBody;

        try
        {
          await _next(context);
        }
        finally
        {
          newBody.Seek(0, SeekOrigin.Begin);
          using (var bodyTextReader = new StreamReader(context.Response.Body))
          {
            responseBodyString = await bodyTextReader.ReadToEndAsync();
            newBody.Seek(0, SeekOrigin.Begin);
            await newBody.CopyToAsync(originalBody);
          }

          Console.WriteLine($"EXTERNAL-API-LOGS:- Method: {method}, Path: {fullPath}, Query: {queryString}, Token: {token}, ApiKey: {apiKey}, RequestBody: {JsonConvert.SerializeObject(requestBodyString)}, Status: {context.Response.StatusCode}, ResponseBody: {responseBodyString}");
        }
      }
      else
      {
        await _next(context);
      }
    }
  }
}

