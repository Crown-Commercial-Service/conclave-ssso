using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Core.DbModel.Entity
{
  // #Auto validation
  public class OrganisationAudit
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Status { get; set; }

    public int OrganisationId { get; set; }

    public string SchemeIdentifier { get; set; }

    public string Action { get; set; }

    public string ActionedBy { get; set; }

    public DateTime CreatedOnUtc { get; set; }
  }
}
