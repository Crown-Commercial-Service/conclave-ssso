using CcsSso.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace CcsSso.Dtos.Domain.Models
{
  public class OrganisationDto
  {
    public int OrganisationId { get; set; }

    public string CiiOrganisationId { get; set; }

    public string OrganisationUri { get; set; }

    public string LegalName { get; set; }

    public bool? RightToBuy { get; set; }

    public string BusinessType { get; set; }

    public int SupplierBuyerType { get; set; }

    public int PartyId { get; set; }

    public Address Address { get; set; }
  }

  public class OrganisationRegistrationDto
  {
    public CiiDto CiiDetails { get; set; }

    public bool RightToBuy { get; set; }

    public string BusinessType { get; set; }

    public int SupplierBuyerType { get; set; }

    public string AdminUserName { get; set; }

    public string AdminUserFirstName { get; set; }

    public string AdminUserLastName { get; set; }
  }

  public class OrganisationRollbackDto
  {
    public string OrganisationId { get; set; }
    public string SchemeName { get; set; }
    public string SchemeNumber { get; set; }

    public string CiiOrganisationId { get; set; }

    public string PhysicalContactId { get; set; }
    public string ContactId { get; set; }
    public string UserId { get; set; }
  }
  public class OrganisationUserDto
  {
    public int Id { get; set; }

    public string UserName { get; set; }

    public string Name { get; set; }

    public int OrganisationId { get; set; }

    public string CiiOrganisationId { get; set; }

    public string OrganisationLegalName { get; set; }
  }
}
