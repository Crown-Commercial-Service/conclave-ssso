using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class Person : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public Party Party { get; set; }

    [ForeignKey("PartyId")]
    public int PartyId { get; set; }

    public int Title { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
  }
}
