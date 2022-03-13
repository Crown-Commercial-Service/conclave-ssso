using CcsSso.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace CcsSso.Dtos.Domain.Models
{
  public class CiiPostResponceDto
  {
    public string OrganisationId { get; set; }
  }

  public class CiiDto
  {
    public string Name { get; set; }

    public CiiContactPoint ContactPoint { get; set; }

    public CiiIdentifier Identifier { get; set; }

    public CiiAddress Address { get; set; }

    public CiiAdditionalIdentifier[] AdditionalIdentifiers { get; set; }
  }

  public class CiiAddress
  {
    public string CountryName { get; set; }

    public string CountryCode { get; set; }

    public string Locality { get; set; }

    public string PostalCode { get; set; }

    public string Region { get; set; }

    public string StreetAddress { get; set; }
  }

  public class CiiContactPoint
  {
    public string Email { get; set; }

    public string FaxNumber { get; set; }

    public string Name { get; set; }

    public string Telephone { get; set; }

    public string Uri { get; set; }
  }

  public class CiiIdentifier
  {
    public string Id { get; set; }

    public string LegalName { get; set; }

    public string Scheme { get; set; }

    public string Uri { get; set; }
  }

  public class CiiAdditionalIdentifier
  {
    public string Id { get; set; }

    public string LegalName { get; set; }

    public string Scheme { get; set; }
  }

  public class CiiSchemeDto
  {
    public string scheme { get; set; }

    public string schemeCountryCode { get; set; }

    public string schemeName { get; set; }
  }

  public class CiiConfig
  {
    public string url { get; set; }
    public string token { get; set; }
    public string deleteToken { get; set; }
    public string clientId { get; set; }
  }
}
