using CcsSso.Core.DbModel.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.DbModel.Entity
{
  public class UserAccessRolePending : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }

    public OrganisationEligibleRole OrganisationEligibleRole { get; set; }

    public int OrganisationEligibleRoleId { get; set; }

    public int Status { get; set; }

    public bool SendEmailNotification { get; set; } = true;
  }
}
