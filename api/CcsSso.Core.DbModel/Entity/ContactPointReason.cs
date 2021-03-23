using System.Collections.Generic;

namespace CcsSso.DbModel.Entity
{
  public class ContactPointReason : BaseEntity
  {
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public List<ContactPoint> ContactPoints { get; set; }
  }
}
