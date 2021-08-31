using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Dtos
{
  public class MfaResetRequest
  {
    public string UserName { get; set; }
    public bool ForceUserSignout { get; set; }
  }

  public class MfaResetInfo
  {
    public string Ticket { get; set; }
  }
}
