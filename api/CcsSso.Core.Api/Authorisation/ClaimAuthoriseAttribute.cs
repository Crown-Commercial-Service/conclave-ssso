using Microsoft.AspNetCore.Authorization;

namespace CcsSso.Core.Authorisation
{
  public class ClaimAuthoriseAttribute : AuthorizeAttribute
  {
    public const string POLICY_PREFIX = "AUTHORISE_CLAIM:";

    public ClaimAuthoriseAttribute(params string[] claims)
    {
      Claims = string.Join(',', claims);
    }

    public string Claims
    {
      get
      {
        return Policy.Substring(POLICY_PREFIX.Length);
      }

      set
      {
        Policy = $"{POLICY_PREFIX}{value}";
      }
    }
  }
}
