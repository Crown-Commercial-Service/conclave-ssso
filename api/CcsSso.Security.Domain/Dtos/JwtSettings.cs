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
}
