using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System;

namespace CcsSso.DbModel.Entity
{
  public class UserGroupMembership : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public OrganisationUserGroup OrganisationUserGroup { get; set; }

    public int OrganisationUserGroupId { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }

    [Required]
    public DateTime MembershipStartDate { get; set; }

    public DateTime MembershipEndDate { get; set; }
  }
}
