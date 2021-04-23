using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CcsSso.Security.Domain.Dtos
{
  public class TokenRequestInfo
  {
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [Required]
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; }

    [Required]
    [JsonPropertyName("redirect_uri")]
    public string RedirectUrl { get; set; }

    [Required]
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("code_verifier")]
    public string CodeVerifier { get; set; }

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }
  }

  public class TokenResponseInfo
  {
    public string AccessToken { get; set; }

    public string IdToken { get; set; }

    public string RefreshToken { get; set; }

    public string SessionState { get; set; }
  }

  public class Tokencontent
  {
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("id_token")]
    public string IdToken { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }
  }

  public class Auth0Tokencontent
  {
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("scope")]
    public string Scope { get; set; }

    [JsonProperty("token_type")]
    public string TokenType { get; set; }
  }

  public class TicketInfo
  {
    [JsonProperty("ticket")]
    public string Ticket { get; set; }
  }
}
