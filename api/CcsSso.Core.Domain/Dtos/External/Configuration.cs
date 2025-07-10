using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
    public class RoleApprovalConfigurationInfo
    {
      public int Id { get; set; }

      public int CcsAccessRoleId { get; set; }

      public int LinkExpiryDurationInMinute { get; set; }

      public string? NotificationEmails { get; set; }

      public DateTime LastUpdatedOnUtc { get; set; }

    }
}
