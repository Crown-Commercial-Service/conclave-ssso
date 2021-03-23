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
    public string OrganisationId { get; set; }

    public OrganisationIdentifier Identifier { get; set; }

    public OrganisationAddress Address { get; set; }

    public OrganisationDetail Detail { get; set; }
  }

  public class OrganisationIdentifier
  {
    public string LegalName { get; set; }

    public string Uri { get; set; }
  }

  public class OrganisationAddress
  {
    public string StreetAddress { get; set; }

    public string Locality { get; set; }

    public string Region { get; set; }

    public string PostalCode { get; set; }

    public string CountryCode { get; set; }
  }

  public class OrganisationDetail
  {
    public string CreationDate { get; set; }

    public string CountryCode { get; set; }

    public string CompanyType { get; set; }

    public bool IsSme { get; set; }

    public bool IsVcse{ get; set; }

    public string Status { get; set; }

    public bool IsActive { get; set; }
  }

  public class OrganisationGroups
  {
    public int GroupId { get; set; }

    public string GroupName { get; set; }
  }
}
