using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Dtos.Wrapper
{
  public class WrapperOrganisationResponse
  {
    public OrganisationIdentifier Identifier { get; set; }

    public List<OrganisationIdentifier> AdditionalIdentifiers { get; set; }

    public OrganisationAddress Address { get; set; }

    public OrganisationResponseDetail Detail { get; set; }
  }

  public class OrganisationIdentifier
  {
    public string Id { get; set; }

    public string LegalName { get; set; }

    public string Uri { get; set; }

    public string Scheme { get; set; }
  }

  public class OrganisationAddress
  {
    public string StreetAddress { get; set; }

    public string Locality { get; set; }

    public string Region { get; set; }

    public string PostalCode { get; set; }

    public string CountryCode { get; set; }
  }

  public class OrganisationResponseDetail
  {
    public string OrganisationId { get; set; }

    public string CreationDate { get; set; }

    public string BusinessType { get; set; }

    public int SupplierBuyerType { get; set; }

    public bool IsSme { get; set; }

    public bool IsVcse { get; set; }

    public bool RightToBuy { get; set; }

    public bool IsActive { get; set; }
  }

  public class WrapperOrganisationRequest
  {
    public OrganisationIdentifier Identifier { get; set; }

    public OrganisationAddress Address { get; set; }

    public OrganisationRequestDetail Detail { get; set; }
  }

  public class OrganisationRequestDetail
  {
    public string OrganisationId { get; set; }

    public string CompanyType { get; set; }

    public int SupplierBuyerType { get; set; }

    public bool IsSme { get; set; }

    public bool IsVcse { get; set; }

    public bool RightToBuy { get; set; }

    public bool IsActive { get; set; }
  }
}
