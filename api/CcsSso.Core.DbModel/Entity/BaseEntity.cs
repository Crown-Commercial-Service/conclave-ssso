using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System;

namespace CcsSso.DbModel.Entity
{
  public class BaseEntity
  {
    public int CreatedUserId { get; set; }

    public int LastUpdatedUserId { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime LastUpdatedOnUtc { get; set; }

    public bool IsDeleted { get; set; }

    [Timestamp]
    public byte[] ConcurrencyKey { get; set; }
  }
}
