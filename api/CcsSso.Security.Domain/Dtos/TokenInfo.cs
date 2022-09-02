using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CcsSso.Security.Domain.Dtos
{
  public class TokenRequestInfo
  {
    public string Code { get; set; }

    public string RefreshToken { get; set; }

    public string GrantType { get; set; }

    public string RedirectUrl { get; set; }

    public string ClientId { get; set; }

    public string Audience { get; set; }

    public string CodeVerifier { get; set; }

    public string ClientSecret { get; set; }

    public string State { get; set; }
    // #Delegated
    public string DelegatedOrgId { get; set; }
  }

  public class TokenResponseInfo
  {
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("id_token")]
    public string IdToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("session_state")]
    public string SessionState { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; set; }
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
