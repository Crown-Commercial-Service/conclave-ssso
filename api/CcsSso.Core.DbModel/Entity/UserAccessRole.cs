using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class UserAccessRole : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }

    public OrganisationEligibleRole OrganisationEligibleRole { get; set; }

    public int OrganisationEligibleRoleId { get; set; }
  }
}
