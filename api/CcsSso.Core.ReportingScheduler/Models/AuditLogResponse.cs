using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ReportingScheduler.Models
{
  public class AuditLogResponse
  {
    public List<AuditLogDetail> AuditLogDetail { get; set; }

    public int CurrentPage { get; set; }
    public int PageCount { get; set; }
    public int RowCount { get; set; }
  }
  public class AuditLogDetail
  {
    public int Id { get; set; }
    public string? Event { get; set; }

    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? Application { get; set; }

    public string? ReferenceData { get; set; }

    public string? IpAddress { get; set; }

    public string? Device { get; set; }

    public DateTime EventTimeUtc { get; set; }
  }
}
