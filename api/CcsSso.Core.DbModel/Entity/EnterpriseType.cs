using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class EnterpriseType : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string EnterpriseTypeName { get; set; }

    public List<OrganisationEnterpriseType> OrganisationEnterpriseTypes { get; set; }
  }
}
