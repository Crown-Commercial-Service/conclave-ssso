using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos
{
  public class OrgJoinNotificationInfo
  {
    public string Email { get; set; }

    public string ToEmail { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string CiiOrganisationId { get; set; }
  }
}
