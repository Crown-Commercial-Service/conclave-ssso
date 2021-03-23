using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class Party : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public PartyType PartyType { get; set; }

    public int PartyTypeId { get; set; }

    public Organisation Organisation { get; set; }

    public User User { get; set; }

    public Person Person { get; set; }

    public List<ContactPoint> ContactPoints { get; set; }
  }
}
