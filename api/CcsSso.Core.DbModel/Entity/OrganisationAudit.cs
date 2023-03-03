using CcsSso.Core.DbModel.Constants;
using CcsSso.DbModel.Entity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Core.DbModel.Entity
{
  public class OrganisationAudit
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public OrgAutoValidationStatus Status { get; set; }

    public int OrganisationId { get; set; }

    public string SchemeIdentifier { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public DateTime ActionedOnUtc { get; set; }

    public Organisation Organisation { get; set; }
  }
}
