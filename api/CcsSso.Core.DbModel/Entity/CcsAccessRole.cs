using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class CcsAccessRole : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string CcsAccessRoleNameKey { get; set; }

    public string CcsAccessRoleName { get; set; }

    public string CcsAccessRoleDescription { get; set; }

    public bool MfaEnabled { get; set; }

    public RoleEligibleOrgType OrgTypeEligibility { get; set; }

    public RoleEligibleSubscriptionType SubscriptionTypeEligibility { get; set; }

    public RoleEligibleTradeType TradeEligibility { get; set; }

    public List<OrganisationEligibleRole> OrganisationEligibleRoles { get; set; }

    public List<ServiceRolePermission> ServiceRolePermissions { get; set; }

    public List<IdamUserLoginRole> IdamUserLoginRoles { get; set; }

    public string DefaultEligibility { get; set; }

    public int ApprovalRequired { get; set; } = 0;
  }
}
