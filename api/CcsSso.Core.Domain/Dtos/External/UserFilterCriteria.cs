using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserFilterCriteria
  {

    [FromQuery(Name = "search-string")]
    public string searchString { get; set; } = null;

    [FromQuery(Name = "delegated-only")]
    public bool isDelegatedOnly { get; set; } = false;

    [FromQuery(Name = "delegated-expired-only")]
    public bool isDelegatedExpiredOnly { get; set; } = false;

    public bool isAdmin { get; set; } = false;

    [FromQuery(Name = "include-unverified-admin")]
    public bool includeUnverifiedAdmin { get; set; } = false;

    [FromQuery(Name = "include-self")]
    public bool includeSelf { get; set; } = false;


  }
}
