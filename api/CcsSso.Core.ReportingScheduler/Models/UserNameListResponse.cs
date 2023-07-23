using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ReportingScheduler.Models
{
  public class UserNameListResponse
  {
    public List<UserNameList> UserNameList { get; set; }
  }

  public class UserNameList
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
}
