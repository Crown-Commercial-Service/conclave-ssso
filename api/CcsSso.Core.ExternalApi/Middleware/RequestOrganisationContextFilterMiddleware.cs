using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Middleware
{
  public class RequestOrganisationContextFilterMiddleware
  {
    private RequestDelegate _next;

    public RequestOrganisationContextFilterMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context, RequestContext requestContext)
    {
      var path = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
      var requestType = path.Contains("organisations") ? RequestType.HavingOrgId : path.Contains("users") ? RequestType.NotHavingOrgId : RequestType.Other;

      // #Delegated
      if (!string.IsNullOrWhiteSpace(context.Request.Query["delegated-organisation-id"]))
      {
        requestType = RequestType.HavingOrgId;
      }

      if (requestType == RequestType.HavingOrgId)
      {
        requestContext.RequestIntendedOrganisationId = context.Request.RouteValues["organisationId"]?.ToString();
        // #Delegated
        if (!string.IsNullOrWhiteSpace(context.Request.Query["delegated-organisation-id"]))
        {
          requestContext.RequestIntendedOrganisationId = context.Request.Query["delegated-organisation-id"];
        }
      }
      else if (requestType == RequestType.NotHavingOrgId)
      {
        // #Delegated
        if (context.Request.Method == "POST" || (context.Request.Method == "PUT" && path.Contains("delegate-user")))// POST request includes the org id in the body
        {
          using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
          {
            var body = await reader.ReadToEndAsync();

            // #Delegated
            if (path.Contains("delegate-user"))
            {
              var userRequestBody = JsonConvert.DeserializeObject<DelegatedUserProfileRequestInfo>(body);
              requestContext.RequestIntendedOrganisationId = userRequestBody.Detail?.DelegatedOrgId;
            }
            else
            {
              var userRequestBody = JsonConvert.DeserializeObject<UserProfileEditRequestInfo>(body);
              requestContext.RequestIntendedOrganisationId = userRequestBody.OrganisationId;
            }
            requestType = RequestType.HavingOrgId;

            // Reset the request body stream position so the next middleware can read it
            context.Request.Body.Position = 0;
          }
        }
        requestContext.RequestIntendedUserName = context.Request.Query["user-id"].ToString();
      }
      requestContext.RequestType = requestType;

      await _next(context);
    }
  }
}
