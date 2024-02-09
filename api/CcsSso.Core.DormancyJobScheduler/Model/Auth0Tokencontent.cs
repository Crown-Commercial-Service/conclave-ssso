using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Model
{
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
}
