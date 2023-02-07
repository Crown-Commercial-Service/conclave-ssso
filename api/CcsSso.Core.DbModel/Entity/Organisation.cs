using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.DbModel.Entity
{
  public class Organisation : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string CiiOrganisationId { get; set; }

    public string LegalName { get; set; }

    public string OrganisationUri { get; set; }

    public bool? RightToBuy { get; set; }

    public string BusinessType { get; set; }

    public int? SupplierBuyerType { get; set; }

    public Party Party { get; set; }

    [ForeignKey("PartyId")]
    public int PartyId { get; set; }

    public CcsService CcsService { get; set; }

    [ForeignKey("CcsServiceId")]
    public int? CcsServiceId { get; set; }

    // TODO - Not clear
    //public int ParentOrganisationId { get; set; }

    public List<OrganisationUserGroup> UserGroups { get; set; }

    public List<OrganisationAccessRole> OrganisationAccessRoles { get; set; }

    public List<OrganisationEligibleRole> OrganisationEligibleRoles { get; set; }

    public List<OrganisationEligibleIdentityProvider> OrganisationEligibleIdentityProviders { get; set; }

    public List<TradingOrganisation> TradingOrganisations { get; set; }

    public List<OrganisationEnterpriseType> OrganisationEnterpriseTypes { get; set; }

    public List<Person> People { get; set; }

    public bool IsActivated { get; set; }

    public bool IsSme { get; set; }

    public bool IsVcse { get; set; }

    public List<OrganisationAudit> OrganisationAudits { get; set; }

    public string DomainName { get; set; }
  }
}
