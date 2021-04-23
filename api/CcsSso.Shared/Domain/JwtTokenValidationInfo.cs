using System.Collections.Generic;

namespace CcsSso.Shared.Domain
{
  public class JwtTokenValidationInfo
  {
    public bool IsValid { get; set; }

    public string Uid { get; set; }

    public string CiiOrgId { get; set; }

    public Dictionary<string, string> ClaimValues { get; set; }
  }
}
