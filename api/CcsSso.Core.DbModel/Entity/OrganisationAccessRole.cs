using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class OrganisationAccessRole : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public string OrganisationAccessRoleName { get; set; }

    public string OrganisationAccessRoleDescription { get; set; }
  }
}
