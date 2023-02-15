using CcsSso.Core.DbModel.Constants;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class OrganisationAuditInfo
  {
    public OrgAutoValidationStatus Status { get; set; }

    public int OrganisationId { get; set; }

    public string SchemeIdentifier { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }
  }

  public class OrganisationAuditResponseInfo
  {
    public string OrganisationId { get; set; }

    public string OrganisationName { get; set; }

    public int OrganisationType { get; set; }

    public DateTime DateOfRegistration { get; set; }

    public bool? RightToBuy { get; set; }
    public OrgAutoValidationStatus? AuditStatus { get; set; }

  }

  public class OrganisationAuditInfoListResponse : PaginationInfo
  {
    public List<OrganisationAuditResponseInfo> OrganisationAuditList { get; set; }
  }


  public class OrganisationAuditFilterCriteria
  {
    [FromQuery(Name = "search-string")]
    public string searchString { get; set; } = null;

    [FromQuery(Name = "pending-only")]
    public bool isPendingOnly { get; set; } = false;

  }
}
