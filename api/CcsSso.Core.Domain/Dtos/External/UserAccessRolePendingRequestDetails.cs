using CcsSso.Core.DbModel.Constants;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserAccessRolePendingRequestDetails
  {
    public List<UserAccessRolePendingDetailsInfo> UserAccessRolePendingDetailsInfo;
  }
  public class UserAccessRolePendingDetailsInfo
  {
    public int Id { get; set; }

    public int UserId { get; set; }

    public int OrganisationEligibleRoleId { get; set; }

    public int Status { get; set; }

    public bool IsDeleted { get; set; }

    public bool SendEmailNotification { get; set; }

    public int? OrganisationUserGroupId { get; set; }

    public int CreatedUserId { get; set; }

    public string CreatedBy { get; set; }

    public string OrganisationId { get; set; }

    public string UserName { get; set; }

    public DateTime LastUpdatedOnUtc { get; set; }

  }
  public class UserAccessRolePendingFilterCriteria
  {
    [FromQuery(Name = "user-ids")]
    public List<int>? UserIds { get; set; }

    [FromQuery(Name = "status")]
    public UserPendingRoleStaus? Status { get; set; }
  }
}
