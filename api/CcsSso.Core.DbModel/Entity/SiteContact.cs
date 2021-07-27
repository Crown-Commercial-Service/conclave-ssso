using CcsSso.Core.DbModel.Constants;
using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class SiteContact : BaseEntity
  {
    public int Id { get; set; }

    // This is the site id (since site is another contact point)
    public int ContactPointId { get; set; }

    public ContactPoint ContactPoint { get; set; }

    // This is the contact id of a site contact. Which is also a contact point. But no navigation included since there is already a relationship 
    public int ContactId { get; set; }

    // If it is assignable OriginalContactId == 0, if this is already an assigned contact OriginalContactId != 0 will have the original contact point id value
    public int OriginalContactId { get; set; }

    public AssignedContactType AssignedContactType { get; set; }
  }
}
