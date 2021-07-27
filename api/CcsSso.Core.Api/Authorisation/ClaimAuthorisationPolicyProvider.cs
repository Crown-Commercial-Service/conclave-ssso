using CcsSso.Core.Domain.Contracts;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace CcsSso.Core.Authorisation
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

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
      if (policyName.StartsWith(ClaimAuthoriseAttribute.POLICY_PREFIX))
      {
        var claimString = policyName.Substring(ClaimAuthoriseAttribute.POLICY_PREFIX.Length);
        var claimList = claimString.Split(',');
        var policyBuilder = new AuthorizationPolicyBuilder();

        var requestContext = _httpContextAccessor.HttpContext.RequestServices.GetService<RequestContext>();
        if (requestContext.UserId == 0) // Requests with api key no authorization
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

        return Task.FromResult(policyBuilder.Build());
      }

      return FallbackPolicyProvider.GetPolicyAsync(policyName);
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
