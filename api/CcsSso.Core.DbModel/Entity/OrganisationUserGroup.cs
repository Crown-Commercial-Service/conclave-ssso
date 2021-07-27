using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class OrganisationUserGroup : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public string UserGroupNameKey { get; set; }

    public string UserGroupName { get; set; }

    public bool MfaEnabled { get; set; }

    public List<UserGroupMembership> UserGroupMemberships { get; set; }

    public List<OrganisationGroupEligibleRole> GroupEligibleRoles { get; set; }
  }
}
