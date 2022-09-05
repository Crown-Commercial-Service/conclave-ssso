using Microsoft.AspNetCore.Mvc;

namespace CcsSso.Security.Api.Models
{
  public class TokenRequest
  {
    [BindProperty(Name = "code")]
    public string Code { get; set; }

    [BindProperty(Name = "refresh_token")]
    public string RefreshToken { get; set; }

    [BindProperty(Name = "grant_type")]
    public string GrantType { get; set; }

    [BindProperty(Name = "redirect_uri")]
    public string RedirectUrl { get; set; }

    [BindProperty(Name = "client_id")]
    public string ClientId { get; set; }

    [BindProperty(Name = "audience")]
    public string Audience { get; set; }

    [BindProperty(Name = "code_verifier")]
    public string CodeVerifier { get; set; }

    [BindProperty(Name = "client_secret")]
    public string ClientSecret { get; set; }

    [BindProperty(Name = "state")]
    public string State { get; set; }
    // #Delegated
    [BindProperty(Name = "delegated_org_id")]
    public string DelegatedOrgId { get; set; }

  }
}
