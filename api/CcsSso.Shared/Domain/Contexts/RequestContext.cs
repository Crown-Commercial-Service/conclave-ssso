using System.Collections.Generic;

namespace CcsSso.Shared.Domain.Contexts
{
  public class RequestContext
  {
    public int UserId { get; set; }

    public string UserName { get; set; }

    public string CiiOrganisationId { get; set; }

    public string IpAddress { get; set; }

    public string Device { get; set; }

    public List<string> Roles { get; set; }
  }
}
