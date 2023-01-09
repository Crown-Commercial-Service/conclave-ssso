using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ExternalApi.Authorisation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Controllers
{
  [Route("users")]
  [ApiController]
  public class UserRolesApprovalController : ControllerBase
  {
    private readonly IUserProfileRoleApprovalService _userProfileRoleApprovalService;
    public UserRolesApprovalController(IUserProfileRoleApprovalService userProfileRoleApprovalService)
    {
      _userProfileRoleApprovalService = userProfileRoleApprovalService;
    }


    [HttpPut("approve/roles")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task<bool> UpdateUserRoleDecision(UserRoleApprovalEditRequest userApprovalRequest)
    {
      return await _userProfileRoleApprovalService.UpdateUserRoleStatusAsync(userApprovalRequest);
    }
  }
}
