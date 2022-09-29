using Newtonsoft.Json;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserFilterCriteria
  {

    [JsonProperty(PropertyName = "search-string")]
    public string searchString { get; set; } = null;

    [JsonProperty(PropertyName = "include-self")]
    public bool includeSelf { get; set; } = false;

    [JsonProperty(PropertyName = "delegated-only")]
    public bool isDelegatedOnly { get; set; } = false;

    [JsonProperty(PropertyName = "delegated-expired-only")]
    public bool isDelegatedExpiredOnly { get; set; } = false;

    [JsonProperty(PropertyName = "isAdmin")]
    public bool isAdmin { get; set; } = false;


  }
}
