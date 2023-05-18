using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ExternalApi.Authorisation;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace CcsSso.ExternalApi.Controllers
{
  [Route("users")]
  [ApiController]
  public class UserDelegationController : ControllerBase
  {
    private readonly IUserProfileService _userProfileService;
    private readonly IDelegationAuditEventService _delegationAuditEventService;

    public UserDelegationController(IUserContactService contactService, IUserProfileService userProfileService,
      IDelegationAuditEventService delegationAuditEventService)
    {
      _userProfileService = userProfileService;
      _delegationAuditEventService = delegationAuditEventService;
    }

    // #Delegated
    #region Delegated access
    /// <summary>
    /// Allows admin to delegate other org user to represent org
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_DETAILS, ERROR_USER_ID_TOO_LONG, ERROR_ORGANISATION_ID_REQUIRED,
    /// INVALID_ROLE, INVALID_USER_DETAIL, INVALID_CII_ORGANISATION_ID, INVALID_USER_DELEGATION_PRIMARY_DETAILS, 
    /// INVALID_USER_DELEGATION, INVALID_USER_DELEGATION_SAME_ORG, ERROR_SENDING_ACTIVATION_LINK
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /delegate-user
    ///     {
    ///       "userName": "user@mail.com",
    ///       "detail": {
    ///         "delegatedOrgId": "organisation id",
    ///         "roleIds": [
    ///           role ids
    ///         ],
    ///         "startDate": "date",
    ///         "endDate": "date"
    ///       }
    ///     }
    ///     
    /// </remarks>
    [HttpPost("delegate-user")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task CreateDelegatedUser(DelegatedUserProfileRequestInfo userProfileRequestInfo)
    {
      await _userProfileService.CreateDelegatedUserAsync(userProfileRequestInfo);
    }

    /// <summary>
    /// Allows admin to update user delegation details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, ERROR_USER_ID_TOO_LONG, ERROR_ORGANISATION_ID_REQUIRED, INVALID_ROLE, INVALID_DETAILS, INVALID_USER_DELEGATION
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /delegate-user
    ///     {
    ///       "userName": "user@mail.com",
    ///       "detail": {
    ///         "delegatedOrgId": "organisation id",
    ///         "roleIds": [
    ///           roles
    ///         ],
    ///         "startDate": "date",
    ///         "endDate": "date"
    ///       }
    ///     }
    ///
    /// </remarks>
    [HttpPut("delegate-user")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task UpdateDelegatedUser(DelegatedUserProfileRequestInfo userProfileRequestInfo)
    {
      await _userProfileService.UpdateDelegatedUserAsync(userProfileRequestInfo);
    }

    /// <summary>
    /// Allows admin to remove/revoke user delegation for organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, ERROR_USER_ID_TOO_LONG, ERROR_ORGANISATION_ID_REQUIRED, INVALID_USER_DELEGATION
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /delegate-user?user-id=user@mail.com&organisation-id='organisation id'
    ///     
    ///
    /// </remarks>
    [HttpDelete("delegate-user")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteDelegatedUser([FromQuery(Name = "user-id")] string userId, [FromQuery(Name = "delegated-organisation-id")] string organisationId)
    {
      await _userProfileService.RemoveDelegatedAccessForUserAsync(userId, organisationId);
    }

    /// <summary>
    /// Validate user acceptation for delegation invitation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_DELEGATION, ERROR_ACTIVATION_LINK_EXPIRED, INVALID_USER_DELEGATION
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    /// GET /delegate-user-acceptance?acceptance-code='code'
    /// 
    /// </remarks>
    [HttpGet("delegate-user-validation")]
    //[ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    //[OrganisationAuthorise("USER")] - No need to add this check for delegation
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task DelegationUserAcceptance([FromQuery(Name = "acceptance-code")] string acceptanceCode)
    {
      await _userProfileService.AcceptDelegationAsync(acceptanceCode);
    }

    /// <summary>
    /// Resend delegation activation link
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_LEGAL_NAME, INVALID_CII_ORGANISATION_ID, ERROR_SENDING_ACTIVATION_LINK
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    /// PUT /delegate-user-resend-activation?user-id=user@mail.com&organisation-id='organisation id'
    /// </remarks>
    [HttpPut("delegate-user-resend-activation")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task ResenedActivationLink([FromQuery(Name = "user-id")] string userId, [FromQuery(Name = "delegated-organisation-id")] string organisationId)
    {
      await _userProfileService.SendUserDelegatedAccessEmailAsync(userId, organisationId, isLogEnable: true);
    }

    [HttpPost("v1/delegate-user")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task CreateDelegatedUserV1(DelegatedUserProfileServiceRoleGroupRequestInfo userProfileRequestInfo)
    {
      await _userProfileService.CreateDelegatedUserV1Async(userProfileRequestInfo);
    }

    [HttpPut("v1/delegate-user")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task UpdateDelegatedUserV1(DelegatedUserProfileServiceRoleGroupRequestInfo userProfileServiceRoleGroupRequestInfo)
    {
      await _userProfileService.UpdateDelegatedUserV1Async(userProfileServiceRoleGroupRequestInfo);
    }

    [HttpGet("v1/delegate-user-auditevents")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("DELEGATION")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task<DelegationAuditEventoServiceRoleGroupInfListResponse> GetDelegationAuditEventsList([FromQuery(Name = "user-id")] string userId, [FromQuery(Name = "delegated-organisation-id")] string organisationId, [FromQuery] ResultSetCriteria resultSetCriteria)
    {
      resultSetCriteria ??= new ResultSetCriteria
      {
        CurrentPage = 1,
        PageSize = 10
      };
      resultSetCriteria.CurrentPage = resultSetCriteria.CurrentPage <= 0 ? 1 : resultSetCriteria.CurrentPage;
      resultSetCriteria.PageSize = resultSetCriteria.PageSize <= 0 ? 10 : resultSetCriteria.PageSize;

      return await _delegationAuditEventService.GetDelegationAuditEventsListAsync(userId, organisationId, resultSetCriteria);
    }
    #endregion

  }
}
