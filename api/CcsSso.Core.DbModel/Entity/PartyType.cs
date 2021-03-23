using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class PartyType : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string PartyTypeName { get; set; }

    public List<Party> Party { get; set; }
  }
}
