using CcsSso.Core.Domain.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Authorisation
{
  public class ClaimAuthorisationPolicyProvider : IAuthorizationPolicyProvider
  {
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimAuthorisationPolicyProvider(IOptions<AuthorizationOptions> options, IHttpContextAccessor httpContextAccessor)
    {
      FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
      _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
      var xapiKey = _httpContextAccessor.HttpContext.Request.Headers["X-API-Key"];

      if (policyName.StartsWith(ClaimAuthoriseAttribute.POLICY_PREFIX))
      {
        var claimString = policyName.Substring(ClaimAuthoriseAttribute.POLICY_PREFIX.Length);
        var claimList = claimString.Split(',');
        var policyBuilder = new AuthorizationPolicyBuilder();

        var requestContext = _httpContextAccessor.HttpContext.RequestServices.GetService<RequestContext>();

        if (!string.IsNullOrEmpty(xapiKey)) //  Requests with api key no authorization
        {
          policyBuilder.RequireAssertion(context => true);
        }
        else
        {
          var authService = _httpContextAccessor.HttpContext.RequestServices.GetService<IAuthService>();

          policyBuilder.RequireAssertion(context =>
          {
            return authService.AuthorizeUser(claimList);
          });
        }

        return await Task.FromResult(policyBuilder.Build());
      }

      else if (policyName.StartsWith(OrganisationAuthoriseAttribute.POLICY_PREFIX))
      {
        var requestType = policyName.Substring(OrganisationAuthoriseAttribute.POLICY_PREFIX.Length);
        var policyBuilder = new AuthorizationPolicyBuilder();

        var requestContext = _httpContextAccessor.HttpContext.RequestServices.GetService<RequestContext>();

        if (!string.IsNullOrEmpty(xapiKey)) //  Requests with api key no authorization
        {
          policyBuilder.RequireAssertion(context => true);
        }
        else
        {
          var authService = _httpContextAccessor.HttpContext.RequestServices.GetService<IAuthService>();

          policyBuilder.RequireAssertion(async context =>
          {
            // #Delegated
            if (requestType == "ORGANISATION" || requestType == "USER_POST" || requestType == "DELEGATION")
            {
              return await authService.AuthorizeForOrganisationAsync(RequestType.HavingOrgId);
            }
            else if (requestType == "USER")
            {
              return await authService.AuthorizeForOrganisationAsync(RequestType.NotHavingOrgId);
            }
            return false;
          });
        }

        return await Task.FromResult(policyBuilder.Build());
      }

      return await FallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
      return ((IAuthorizationPolicyProvider)FallbackPolicyProvider).GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
    {
      return ((IAuthorizationPolicyProvider)FallbackPolicyProvider).GetFallbackPolicyAsync();
    }
  }
}
