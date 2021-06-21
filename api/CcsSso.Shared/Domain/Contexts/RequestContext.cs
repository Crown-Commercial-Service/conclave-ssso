using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain.Contexts
{
  public class RequestContext
  {
    public int UserId { get; set; }

    public string CiiOrganisationId { get; set; }

    public string IpAddress { get; set; }

    public string Device { get; set; }
  }
}
