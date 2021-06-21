using Microsoft.AspNetCore.Http;
using System.Linq;

namespace CcsSso.Shared.Extensions
{
  public static class HttpContextExtensions
  {
    /// <summary>
    /// Extension method to get remote ip address of request (for apps hosted in Cloud Foundry)
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetRemoteIPAddress(this HttpContext context)
    {
      // "X-Forwarded-For" has format of public ip of request, private ip
      // "CF-Connecting-IP" at the moment doesn't return any value keep it now for future references
      // If nothing fallback to "RemoteIpAddress", this is not correct in the CF
      return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0] ?? context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
        context.Connection.RemoteIpAddress?.ToString();
    }
  }
}
