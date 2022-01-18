using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Jobs
{
  public class BulkUploadMigrationResult
  {
    public bool IsCompleted { get; set; }

    public int TotalOrganisationCount { get; set; }

    public int TotalUserCount { get; set; }

    public int ProceededUserCount { get; set; }

    public int FailedUserCount { get; set; }
  }
}
