using CcsSso.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace CcsSso.Dtos.Domain.Models
{
  public class ContactDetailDto
  {
    public int ContactId { get; set; }

    public int PartyId { get; set; }

    public int OrganisationId { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string TeamName { get; set; }

    public string PhoneNumber { get; set; }

    public string Fax { get; set; }

    public string WebUrl { get; set; }

    // [Required]
    public ContactType ContactType { get; set; }

    public string ContactReason { get; set; }

    public Address Address { get; set; }
  }

  public class ContactDAModel
  {
    public int PartyId { get; set; }

    public int ContactPointId { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string TeamName { get; set; }

    public string PhoneNumber { get; set; }

    public Address Address { get; set; }
  }

  public class Address
  {
    public string StreetAddress { get; set; }

    public string Locality { get; set; }

    public string Region { get; set; }

    public string PostalCode { get; set; }

    public string CountryCode { get; set; }

    public string Uprn { get; set; }
  }
}
