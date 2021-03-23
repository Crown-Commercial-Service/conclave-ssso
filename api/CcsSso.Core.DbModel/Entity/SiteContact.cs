using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DbModel.Entity
{
  public class SiteContact : BaseEntity
  {
    public int Id { get; set; }

    public int ContactPointId { get; set; }

    public ContactPoint ContactPoint { get; set; }

    public int ContactId { get; set; }
  }
}
