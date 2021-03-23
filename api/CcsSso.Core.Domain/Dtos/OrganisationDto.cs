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

    public bool RightToBuy { get; set; }

    public int PartyId { get; set; }

    public ContactDetailDto ContactPoint { get; set; }

    public Address Address { get; set; }
  }
}
