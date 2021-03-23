using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class TradingOrganisation : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public string TradingName { get; set; }
  }
}
