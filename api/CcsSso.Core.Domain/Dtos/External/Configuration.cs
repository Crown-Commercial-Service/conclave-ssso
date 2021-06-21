using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class CcsServiceInfo
  {
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Code { get; set; }

    public string Url { get; set; }
  }
}
