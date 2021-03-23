using CcsSso.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace CcsSso.Dtos.Domain.Models
{
  public class CiiDto
  {
    public string name { get; set; }

    public CiiContactPoint contactPoint { get; set; }

    public CiiIdentifier identifier { get; set; }

    public CiiAddress address { get; set; }

    public CiiAdditionalIdentifier[] additionalIdentifiers { get; set; }
  }

  public class CiiAddress
  {
    public string countryName { get; set; }

    public string locality { get; set; }

    public string postalCode { get; set; }

    public string region { get; set; }

    public string streetAddress { get; set; }
  }

  public class CiiContactPoint
  {
    public string email { get; set; }

    public string faxNumber { get; set; }

    public string name { get; set; }

    public string telephone { get; set; }

    public string uri { get; set; }
  }

  public class CiiIdentifier
  {
    public string id { get; set; }

    public string legalName { get; set; }

    public string scheme { get; set; }

    public string uri { get; set; }
  }

  public class CiiAdditionalIdentifier
  {
    public string id { get; set; }

    public string legalName { get; set; }

    public string scheme { get; set; }
  }

  public class CiiSchemeDto
  {
    public string scheme { get; set; }

    public string schemeCountryCode { get; set; }

    public string schemeName { get; set; }
  }

  public class CiiOrg
  {
    public string ccsOrgId { get; set; }
  }

  public class CiiPutDto
  {
    public string ccsOrgId { get; set; }

    public CiiIdentifier identifier { get; set; }
  }
}
