using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Model
{
    public class OrganisationLogDetail:OrganisationDetail
    {
      public string AdminEmail { get; set; }
      public string AutovalidationStatus { get; set; }
      public string DateTime { get; set; }
    public string?  Information { get; set; }

  }
  
}
