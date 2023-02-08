using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ServiceOnboardingScheduler.Model
{
  public class OrganizationDetails
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string AdminEmail { get; set; }
    public string AutovalidationStatus { get; set; }
    public string DateTime { get; set; }

   
  }
}
