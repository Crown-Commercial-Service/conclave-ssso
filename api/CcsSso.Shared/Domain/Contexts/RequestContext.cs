using CcsSso.Shared.Domain.Constants;
using System.Collections.Generic;

namespace CcsSso.Shared.Domain.Contexts
{
  public class RequestContext
  {
    public int UserId { get; set; }

    public string ServiceClientId { get; set; }

    public int ServiceId { get; set; }

    public string UserName { get; set; }

    public string CiiOrganisationId { get; set; }

    public string IpAddress { get; set; }

    public string Device { get; set; }

    public List<string> Roles { get; set; }

    public RequestType RequestType { get; set; }

    public string RequestIntendedOrganisationId { get; set; }

    public string RequestIntendedUserName { get; set; }

    // #Deleated: To manage delegate user search
    public bool IsDelegated { get; set; }

    public string apiKey { get; set; }
  }
}
