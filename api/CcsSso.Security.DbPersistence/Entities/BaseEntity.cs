using System;
using System.ComponentModel.DataAnnotations;

namespace CcsSso.Security.DbPersistence.Entities
{
  public class BaseEntity
  {
    public int Id { get; set; }

    public int CreatedByUserId { get; set; }

    public int LastUpdatedByUserId { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime LastUpdatedOnUtc { get; set; }

    public bool IsDeleted { get; set; }

    [Timestamp]
    public byte[] ConcurrencyKey { get; set; }
  }
}
