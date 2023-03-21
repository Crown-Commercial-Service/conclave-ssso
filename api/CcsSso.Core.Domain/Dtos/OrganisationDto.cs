using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using System;
using System.Collections.Generic;
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

    public bool IsAutovalidationPending { get; set; }
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

    public bool IsAdmin { get; set; } = false;
  }

  public class OrganisationUserListResponse : PaginationInfo
  {
    public List<OrganisationUserDto> OrgUserList { get; set; }
  }

  public class OrganisationListResponse : PaginationInfo
  {
    public List<OrganisationDto> OrgList { get; set; }
  }

  public class OrganisationJoinRequest
  {
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string CiiOrgId { get; set; }

    public string ErrorCode { get; set; } = string.Empty;
  }
}
