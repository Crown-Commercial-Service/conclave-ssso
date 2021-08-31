using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos
{
  public class MfaResetInfo
  {
    public string UserName { get; set; }

    public string Ticket { get; set; }
  }
}
