using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ReportingScheduler.Models
{
  public class ContactModel
  {
    public string ContactType { get; set; }
    public object DetectedContact { get; set; } 
  }

  public class ContactDetailModel
  {
    public string ContactType { get; set; }
    public dynamic ContactDetail { get; set; }
  }
}
