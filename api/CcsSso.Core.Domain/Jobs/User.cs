using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Jobs
{
  public class IdamUser
  {
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public bool EmailVerified { get; set; }
  }
}
