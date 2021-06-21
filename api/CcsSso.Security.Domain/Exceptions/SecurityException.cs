using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace CcsSso.Security.Domain.Exceptions
{
  public class SecurityException : Exception
  {
    public SecurityException(ErrorInfo errorInfo)
        : base(JsonConvert.SerializeObject(errorInfo))
    {
    }
  }

  public class ErrorInfo
  {
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;

    [JsonPropertyName("error_uri")]
    public string ErrorUri { get; set; } = string.Empty;
  }
}
