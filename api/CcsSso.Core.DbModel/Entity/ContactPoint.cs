using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class ContactPoint : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Party Party { get; set; }

    public int PartyId { get; set; }

    public PartyType PartyType { get; set; }

    public int PartyTypeId { get; set; }

    public ContactDetail ContactDetail { get; set; }

    public int ContactDetailId { get; set; }

    public ContactPointReason ContactPointReason { get; set; }

    public int ContactPointReasonId { get; set; }

    public string SiteName { get; set; }

    public bool IsSite { get; set; }

    public List<SiteContact> SiteContacts { get; set; }

    // Original contact point id of assigned contact. Value is 0 for non assigned contacts
    public int OriginalContactPointId { get; set; }

    public AssignedContactType AssignedContactType { get; set; }
  }
}
