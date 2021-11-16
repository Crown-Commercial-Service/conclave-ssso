using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Dtos
{
  public class ServiceProfile
  {
    public int ServiceId { get; set; }

    public string Audience { get; set; }

    public List<string> RoleKeys { get; set; }
  }
}
