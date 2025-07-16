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

		public DateTime CreatedOnUtc { get; set; }

		public Address Address { get; set; }

    public bool IsAutovalidationPending { get; set; }
  }

  public class OrganisationListResponse : PaginationInfo
  {
    public List<OrganisationDto> OrgList { get; set; }
  }

  
}
