using CcsSso.Core.DbModel.Constants;
using CcsSso.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{

  public class OrganisationRole
  {
    public int RoleId { get; set; }

    public int CcsAccessRoleId { get; set; }

    public string RoleKey { get; set; }

    public string RoleName { get; set; }

    public string ServiceName { get; set; }

    public RoleEligibleOrgType OrgTypeEligibility { get; set; }

    public RoleEligibleSubscriptionType SubscriptionTypeEligibility { get; set; }

    public RoleEligibleTradeType TradeEligibility { get; set; }

    public int[] AutoValidationRoleTypeEligibility { get; set; }
  }

  public class OrganisationRoleUpdate
  {
    public bool IsBuyer { get; set; }
    public List<OrganisationRole> RolesToAdd { get; set; }
    public List<OrganisationRole> RolesToDelete { get; set; }
  }

  // #Auto validation
  public class AutoValidationDetails
  {
    public string AdminEmailId { get; set; }
    public bool IsFromBackgroundJob { get; set; } = false;
    public string? CompanyHouseId { get; set; }
  }

  public class AutoValidationOneTimeJobDetails : AutoValidationDetails
  {
    public bool isDomainValid { get; set; } = false;
  }


  #region ServiceRoleGroup
  public class ServiceRoleGroup : ServiceRoleGroupInfo
  {
    public RoleEligibleOrgType OrgTypeEligibility { get; set; }

    public RoleEligibleSubscriptionType SubscriptionTypeEligibility { get; set; }

    public RoleEligibleTradeType TradeEligibility { get; set; }

    public int DisplayOrder { get; set; }

    public string Description { get; set; }

    public int[] AutoValidationRoleTypeEligibility { get; set; } = { };
  }


  #endregion

  public class InactiveOrganisationResponse
  {
    public string OrganisationId { get; set; }
    public int SupplierBuyerType { get; set; }
  }

}
