namespace CcsSso.Domain.Dtos
{
  public class ApplicationConfigurationInfo
  {
    public JwtTokenValidationInfo JwtTokenValidationInfo { get; set; }
  }

  public class JwtTokenValidationInfo
  {
    public string IdamClienId { get; set; }

    public string Issuer { get; set; }

    public string JwksUrl { get; set; }
  }
}
