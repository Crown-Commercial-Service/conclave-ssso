using CcsSso.Core.DbModel.Constants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class CcsServiceRoleGroup : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Key { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public RoleEligibleOrgType OrgTypeEligibility { get; set; }

    public RoleEligibleSubscriptionType SubscriptionTypeEligibility { get; set; }

    public RoleEligibleTradeType TradeEligibility { get; set; }

    public bool MfaEnabled { get; set; }

    public string DefaultEligibility { get; set; }

    public int ApprovalRequired { get; set; } = 0;

    public int DisplayOrder { get; set; }

    public List<CcsServiceRoleMapping> CcsServiceRoleMappings { get; set; }

  }
}