namespace CcsSso.Security.Domain.Dtos
{
  public class AuthResultDto
  {
    public string Error { get; set; }

    public bool ChallengeRequired { get; set; }

    public string ChallengeName { get; set; }

    public string IdToken { get; set; }

    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }
  }
}
