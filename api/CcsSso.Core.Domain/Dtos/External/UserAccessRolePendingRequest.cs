using CcsSso.Core.DbModel.Constants;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserAccessRolePendingRequestDetails
  {
    public List<UserAccessRolePendingDetailsInfo> UserAccessRolePendingDetails;
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

    public int OrganisationId { get; set; }
  }

  public class UserAccessRolePendingFilterCriteria
  {
    [FromQuery(Name = "User-Ids")]
    public List<int>? UserIds { get; set; }

    [FromQuery(Name = "Status")]
    public UserPendingRoleStaus? Status { get; set; }
  }

  public class UserResponse
  {
    public int Id { get; set; }
    public string UserName { get; set; }
  }

}
