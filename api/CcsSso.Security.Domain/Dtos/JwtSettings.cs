using System.Security.Claims;

namespace CcsSso.Security.Domain.Dtos
{
  public class JwtSettings
  {
    public bool ValidateIssuer { get; set; }

    public string Issuer { get; set; }

    public bool ValidateAudience { get; set; }

    public string Audience { get; set; }

    public string JWTKeyEndpoint { get; set; }
  }

  public class ClaimInfo
  {
    public ClaimInfo(string key, string value, string valueType = null)
    {
      Key = key;
      Value = value;
      ValueType = valueType ?? ClaimValueTypes.String;
    }

    public string Key { get; }

    public string Value { get; }

    public string ValueType { get; }
  }
}
