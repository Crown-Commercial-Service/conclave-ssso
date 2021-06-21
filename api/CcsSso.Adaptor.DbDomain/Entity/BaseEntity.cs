using System;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  public class BaseEntity
  {
    public int Id { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime LastUpdatedOnUtc { get; set; }

    public bool IsDeleted { get; set; }
  }
}
