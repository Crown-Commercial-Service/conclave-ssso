using CcsSso.DbModel.Entity;
using System;

namespace CcsSso.Core.DbModel.Entity
{
  public class AuditLog
  {
    public int Id { get; set; }

    public string Event { get; set; }

    public int UserId { get; set; }

    public string Application { get; set; }

    public string ReferenceData { get; set; }

    public string IpAddress { get; set; }

    public string Device { get; set; }

    public DateTime EventTimeUtc { get; set; }
  }
}
