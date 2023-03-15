using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ExternalApi.Authorisation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
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
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task<bool> UpdateUserRoleDecision(UserRoleApprovalEditRequest userApprovalRequest)
    {
      return await _userProfileRoleApprovalService.UpdateUserRoleStatusAsync(userApprovalRequest);
    }

    /// <summary>
    /// Retrieve user all roles which are in pending for approval
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="400">Bad request.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET approval/roles/pending?user-id=user@mail.com
    ///
    /// </remarks>
    [HttpGet("approve/roles")]
    [ClaimAuthorise("ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserAccessRolePendingDetails), 200)]
    public async Task<List<UserAccessRolePendingDetails>> GetUserRolesPendingForApproval([FromQuery(Name = "user-id")] string userId)
    {
      return await _userProfileRoleApprovalService.GetUserRolesPendingForApprovalAsync(userId);
    }

    /// <summary>
    /// Delete user roles which are in pending for approval
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_ROLE_INFO
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /approve/role?user-id=user@mail.com&roles=1,2
    ///     
    ///
    /// </remarks>
    [HttpDelete("approve/roles")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task RemoveApprovalPendingRoles([FromQuery(Name = "user-id")] string userId, [FromQuery(Name = "roles")] string roleIds)
    {
      await _userProfileRoleApprovalService.RemoveApprovalPendingRolesAsync(userId, roleIds);
    }

    /// <summary>
    /// Validate role approval token and return details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="400">Bad request.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET approve/verify?token=encryptedtoken
    ///
    /// </remarks>
    [HttpGet("approve/verify")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserAccessRolePendingTokenDetails), 200)]
    public async Task<UserAccessRolePendingTokenDetails> VerifyRoleApprovalToken([FromQuery(Name = "token")] string token)
    {
      return await _userProfileRoleApprovalService.VerifyAndReturnRoleApprovalTokenDetailsAsync(token);
    }

    /// <summary>
    /// Allows a user to create user roles which required approval
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_ROLE, INVALID_USER_DETAIL
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /users
    ///     {
    ///        "userName": "user@mail.com",
    ///        "detail": {
    ///           "roleIds": { 1, 2 }
    ///        }
    ///     }
    ///
    /// </remarks>
    [HttpPost("approve/roles")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER_POST")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task CreateUserRolesPendingForApproval(UserProfileEditRequestInfo userProfileRequestInfo)
    {
      await _userProfileRoleApprovalService.CreateUserRolesPendingForApprovalAsync(userProfileRequestInfo);
    }

    /// <summary>
    /// Retrieve user all service role groups which are in pending for approval
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="400">Bad request.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET approval/servicerolegroups/pending?user-id=user@mail.com
    ///
    /// </remarks>
    [HttpGet("approve/servicerolegroups")]
    [ClaimAuthorise("ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserServiceRoleGroupPendingDetails), 200)]
    public async Task<List<UserServiceRoleGroupPendingDetails>> GetUserServiceRoleGroupsPendingForApproval([FromQuery(Name = "user-id")] string userId)
    {
      return await _userProfileRoleApprovalService.GetUserServiceRoleGroupsPendingForApprovalAsync(userId);
    }

    /// <summary>
    /// Validate role approval token and return serice role group details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="400">Bad request.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET approve/servicerolegroup/verify?token=encryptedtoken
    ///
    /// </remarks>
    [HttpGet("approve/servicerolegroup/verify")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserAccessServiceRoleGroupPendingTokenDetails), 200)]
    public async Task<UserAccessServiceRoleGroupPendingTokenDetails> VerifyServiceRoleGroupApprovalToken([FromQuery(Name = "token")] string token)
    {
      return await _userProfileRoleApprovalService.VerifyAndReturnServiceRoleGroupApprovalTokenDetailsAsync(token);
    }
  }
}
