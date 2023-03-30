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
  public class OrganisationProfileInfo
  {
    public OrganisationIdentifier Identifier { get; set; }

    public OrganisationAddress Address { get; set; }

    public OrganisationDetail Detail { get; set; }
  }

  public class OrganisationProfileResponseInfo
  {
    public OrganisationIdentifier Identifier { get; set; }

    public List<OrganisationIdentifier> AdditionalIdentifiers { get; set; }

    public OrganisationAddressResponse Address { get; set; }

    // Commented since this is still not available from CII service
    //public OrganisationContactPoint ContactPoint { get; set; }

    public OrganisationDetail Detail { get; set; }
  }

  public class OrganisationIdentifier
  {
    public string Id { get; set; }

    public string LegalName { get; set; }

    public string Uri { get; set; }

    public string Scheme { get; set; }
  }

  public class OrganisationAddressResponse
  {
    public string StreetAddress { get; set; }

    public string Locality { get; set; }

    public string Region { get; set; }

    public string PostalCode { get; set; }

    public string CountryCode { get; set; }

    public string CountryName { get; set; }
  }

  public class OrganisationAddress
  {
    public string StreetAddress { get; set; }

    public string Locality { get; set; }

    public string Region { get; set; }

    public string PostalCode { get; set; }

    public string CountryCode { get; set; }
  }

  public class OrganisationContactPoint
  {
    public string Email { get; set; }

    public string FaxNumber { get; set; }

    public string Name { get; set; }

    public string Telephone { get; set; }

    public string Uri { get; set; }
  }

  public class OrganisationDetail
  {
    public string OrganisationId { get; set; }

    public string CreationDate { get; set; }

    public string BusinessType { get; set; }

    public int SupplierBuyerType { get; set; }

    public bool IsSme { get; set; }

    public bool IsVcse{ get; set; }

    public bool RightToBuy { get; set; }

    public bool IsActive { get; set; }

    public string DomainName { get; set; }
  }

  public class OrganisationRole
  {
    public int RoleId { get; set; }

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

  public class OrganisationAutoValidationRoleUpdate
  {
    // -1 set if not passed to make invalid org type
    public RoleEligibleTradeType OrgType { get; set; } = (RoleEligibleTradeType)(-1);
    public List<OrganisationRole> RolesToAdd { get; set; }
    public List<OrganisationRole> RolesToDelete { get; set; }
    public List<OrganisationRole> RolesToAutoValid { get; set; }
    public string? CompanyHouseId { get; set; }
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

  public class OrganisationServiceRoleGroupUpdate
  {
    public bool IsBuyer { get; set; }
    public List<int> ServiceRoleGroupsToAdd { get; set; }
    public List<int> ServiceRoleGroupsToDelete { get; set; }
  }

  public class OrgAutoValidServiceRoleGroupUpdate
  {
    // -1 set if not passed to make invalid org type
    public RoleEligibleTradeType OrgType { get; set; } = (RoleEligibleTradeType)(-1);
    public List<int> ServiceRoleGroupsToAdd { get; set; }
    public List<int> ServiceRoleGroupsToDelete { get; set; }
    public List<int> ServiceRoleGroupsToAutoValid { get; set; }
    public string? CompanyHouseId { get; set; }
  }

  #endregion

}
