using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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

  public class JsonWebKeyInfo
  {
    [JsonProperty("alg")]
    public string Alg { get; set; }

    [JsonProperty("kty")]
    public string Kty { get; set; }

    [JsonProperty("use")]
    public string Use { get; set; }

    [JsonProperty("n")]
    public string N { get; set; }

    [JsonProperty("e")]
    public string E { get; set; }

    [JsonProperty("kid")]
    public string Kid { get; set; }
  }

  public class JsonWebKeySetInfo
  {
    public List<JsonWebKeyInfo> Keys { get; set; }
  }
}
