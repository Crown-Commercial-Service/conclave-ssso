using System.Collections.Generic;

namespace CcsSso.Shared.Domain.Dto
{
  public class OrganisationProfileResponseInfo
  {
    public OrganisationIdentifier Identifier { get; set; }

    public List<OrganisationIdentifier> AdditionalIdentifiers { get; set; }

    public OrganisationAddressResponse Address { get; set; }

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
  }


}
