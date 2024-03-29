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
  public class UserProfileController : ControllerBase
  {
    private readonly IUserProfileService _userProfileService;
    private readonly IUserContactService _contactService;
    private readonly IDelegationAuditEventService _delegationAuditEventService;

    public UserProfileController(IUserContactService contactService, IUserProfileService userProfileService,
      IDelegationAuditEventService delegationAuditEventService)
    {
      _contactService = contactService;
      _userProfileService = userProfileService;
      _delegationAuditEventService = delegationAuditEventService;
    }

    #region User profile

    /// <summary>
    /// Allows a user to create user details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_FIRST_NAME, INVALID_LAST_NAME, INVALID_USER_GROUP_ROLE,ERROR_PASSWORD_TOO_WEAK INVALID_USER_GROUP, INVALID_ROLE, INVALID_IDENTITY_PROVIDER, INVALID_USER_DETAIL
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /users
    ///     {
    ///        "firstName": "FirstName",
    ///        "lastName": "LastName",
    ///        "userName": "user@mail.com",
    ///        "password":"",// Not mandatory
    ///        "SendUserRegistrationEmail":false,
    ///        "organisationId": "CcsOrgId1",
    ///        "detail": {
    ///           "id": 0,
    ///           "groupIds": { 1, 2 },
    ///           "roleIds": { 1, 2 },
    ///           "identityProviderId": 1,
    ///        }
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER_POST")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task<UserEditResponseInfo> CreateUser(UserProfileEditRequestInfo userProfileRequestInfo)
    {
      return await _userProfileService.CreateUserAsync(userProfileRequestInfo);
    }

    /// <summary>
    /// Allows a user to retrieve details for a given user
    /// #Delegated
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /users?user-id=user@mail.com&is-delegated=true&is-delegated-search=true&delegated-organisation-id=123
    ///     
    ///
    /// </remarks>
    [HttpGet]
    [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR", "ORG_DEFAULT_USER", "DELEGATED_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserProfileResponseInfo), 200)]
    public async Task<UserProfileResponseInfo> GetUser([FromQuery(Name = "user-id")] string userId, [FromQuery(Name = "is-delegated")] bool isDelegated = false, [FromQuery(Name = "is-delegated-search")] bool isSearchUser = false, [FromQuery(Name = "delegated-organisation-id")] string delegatedOrgId = "")
    {
      return await _userProfileService.GetUserAsync(userId, isDelegated, isSearchUser, delegatedOrgId);
    }

    /// <summary>
    /// Allows a user to update user details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_FIRST_NAME, INVALID_LAST_NAME, INVALID_USER_GROUP_ROLE, INVALID_USER_GROUP, INVALID_ROLE, INVALID_IDENTITY_PROVIDER, ERROR_CANNOT_REMOVE_ADMIN_ROLE_OR_GROUP_OF_LAST_ADMIN, INVALID_USER_DETAIL
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /users?user-id=user@mail.com
    ///     {
    ///        "firstName": "FirstName",
    ///        "lastName": "LastName",
    ///        "organisationId": "CcsOrgId1",
    ///        "userName": "user@mail.com",
    ///        "detail": {
    ///           "id": 1,
    ///           "groupIds": { 1, 2},
    ///           "roleIds": { 1, 2 },
    ///           "identityProviderId": 1,
    ///        }
    ///     }
    ///
    /// </remarks>
    [HttpPut]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task<UserEditResponseInfo> UpdateUser([FromQuery(Name = "user-id")] string userId, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      return await _userProfileService.UpdateUserAsync(userId, userProfileRequestInfo);
    }


    /// <summary>
    /// Updates user account verify state
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="400">Bad request.
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /account-verification?user-id=123
    ///     {
    ///     }
    ///
    /// </remarks>
    [HttpPut("account-verification")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task VerifyAccount([FromQuery(Name = "user-id")] string userId)
    {
      await _userProfileService.VerifyUserAccountAsync(userId);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, ERROR_CANNOT_DELETE_LAST_ADMIN_OF_ORGANISATION
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /users?user-id=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpDelete]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUser([FromQuery(Name = "user-id")] string userId)
    {
      await _userProfileService.DeleteUserAsync(userId);
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpPut("passwords")]
    [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task ResetUserPassword([FromQuery(Name = "user-id")] string userId, string? component)
    {
      await _userProfileService.ResetUserPasswodAsync(userId, component);
    }

    [HttpDelete("admin-roles")]
    [ClaimAuthorise("ORG_USER_SUPPORT")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task RemoveAdminRoles([FromQuery(Name = "user-id")] string userId)
    {
      await _userProfileService.RemoveAdminRolesAsync(userId);
    }

    [HttpPut("admin-roles")]
    [ClaimAuthorise("ORG_USER_SUPPORT")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task AddAdminRole([FromQuery(Name = "user-id")] string userId)
    {
      await _userProfileService.AddAdminRoleAsync(userId);
    }

    #endregion

    #region User contact
    /// <summary>
    /// Allows a user to create user contact details
    /// </summary>
    /// <response  code="200">Ok. Return created contact id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INSUFFICIENT_DETAILS, INVALID_EMAIL, INVALID_PHONE_NUMBER, INVALID_CONTACT_TYPE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /users/contacts?user-id=user@mail.com
    ///     {
    ///        "contactPointReason": "BILLING/SHIPPING",
    ///        "contactPointName": "Test User",
    ///        "contacts": [
    ///           {
    ///             contactType: "EMAIL",
    ///             contactValue: "testuser@mail.com"
    ///           },
    ///           {
    ///             contactType: "PHONE",
    ///             contactValue: "+551155256325"
    ///           },
    ///           {
    ///             contactType: "FAX",
    ///             contactValue: "+551155256325"
    ///           },
    ///           {
    ///             contactType: "WEB_ADDRESS",
    ///             contactValue: "test.com"
    ///           },
    ///        ]
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPost("contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateUserContact([FromQuery(Name = "user-id")] string userId, ContactRequestInfo contactInfo)
    {
      return await _contactService.CreateUserContactAsync(userId, contactInfo);
    }

    /// <summary>
    /// Allows a user to get user contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /user-profile/users/contacts?user-id=user@mail.com
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(UserContactInfoList), 200)]
    public async Task<UserContactInfoList> GetUserContactsList([FromQuery(Name = "user-id")] string userId, [FromQuery] string contactType)
    {
      return await _contactService.GetUserContactsListAsync(userId, contactType);
    }

    /// <summary>
    /// Allows a user to retrieve details for a given contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /users/contacts/1?user-id=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpGet("contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(UserContactInfo), 200)]
    public async Task<UserContactInfo> GetUserContact([FromQuery(Name = "user-id")] string userId, int contactId)
    {
      return await _contactService.GetUserContactAsync(userId, contactId);
    }


    /// <summary>
    /// Allows a user to edit user contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INSUFFICIENT_DETAILS, INVALID_EMAIL, INVALID_PHONE_NUMBER, INVALID_CONTACT_TYPE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /users/contacts/1?user-id=user@mail.com
    ///     {
    ///        "contactPointReason": "BILLING/SHIPPING",
    ///        "contactPointName": "Test User",
    ///        "contacts": [
    ///           {
    ///             contactType: "EMAIL",
    ///             contactValue: "testuser@mail.com"
    ///           },
    ///           {
    ///             contactType: "PHONE",
    ///             contactValue: "+551155256325"
    ///           },
    ///           {
    ///             contactType: "FAX",
    ///             contactValue: "+551155256325"
    ///           },
    ///           {
    ///             contactType: "WEB_ADDRESS",
    ///             contactValue: "test.com"
    ///           },
    ///        ]
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPut("contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateUserContact([FromQuery(Name = "user-id")] string userId, int contactId, ContactRequestInfo contactInfo)
    {
      await _contactService.UpdateUserContactAsync(userId, contactId, contactInfo);
    }

    /// <summary>
    /// Remove a contact from user
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /users/contacts/1?user-id=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpDelete("contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUserContact([FromQuery(Name = "user-id")] string userId, int contactId)
    {
      await _contactService.DeleteUserContactAsync(userId, contactId);
    }
    #endregion        

    #region User profile version 1

    /// <summary>
    /// Allows a user to retrieve details for a given user
    /// #Delegated
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_SERVICE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /users/v1?user-id=user@mail.com&is-delegated=true&is-delegated-search=true&delegated-organisation-id=123
    ///     
    ///
    /// </remarks>
    [HttpGet("v1")]
    [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR", "ORG_DEFAULT_USER", "DELEGATED_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserProfileServiceRoleGroupResponseInfo), 200)]
    public async Task<UserProfileServiceRoleGroupResponseInfo> GetUserV1([FromQuery(Name = "user-id")] string userId, [FromQuery(Name = "is-delegated")] bool isDelegated = false, [FromQuery(Name = "is-delegated-search")] bool isSearchUser = false, [FromQuery(Name = "delegated-organisation-id")] string delegatedOrgId = "")
    {
      return await _userProfileService.GetUserV1Async(userId, isDelegated, isSearchUser, delegatedOrgId);
    }

    /// <summary>
    /// Allows a user to create user details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_FIRST_NAME, INVALID_LAST_NAME, INVALID_USER_GROUP_ROLE,ERROR_PASSWORD_TOO_WEAK INVALID_USER_GROUP, INVALID_ROLE, INVALID_IDENTITY_PROVIDER, INVALID_USER_DETAIL, INVALID_SERVICE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /users/v1
    ///     {
    ///        "firstName": "FirstName",
    ///        "lastName": "LastName",
    ///        "userName": "user@mail.com",
    ///        "password":"",// Not mandatory
    ///        "SendUserRegistrationEmail":false,
    ///        "organisationId": "CcsOrgId1",
    ///        "detail": {
    ///           "id": 0,
    ///           "groupIds": { 1, 2 },
    ///           "serviceRoleGroupIds": { 1, 2 },
    ///           "identityProviderId": 1,
    ///        }
    ///     }
    ///
    /// </remarks>
    [HttpPost("v1")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER_POST")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task<UserEditResponseInfo> CreateUserV1(UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo)
    {
      return await _userProfileService.CreateUserV1Async(userProfileServiceRoleGroupEditRequestInfo);
    }

    /// <summary>
    /// Allows a user to update user details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INVALID_FIRST_NAME, INVALID_LAST_NAME, INVALID_USER_GROUP_ROLE, INVALID_USER_GROUP, INVALID_ROLE, INVALID_IDENTITY_PROVIDER, ERROR_CANNOT_REMOVE_ADMIN_ROLE_OR_GROUP_OF_LAST_ADMIN, INVALID_USER_DETAIL, INVALID_SERVICE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /users/v1?user-id=user@mail.com
    ///     {
    ///        "firstName": "FirstName",
    ///        "lastName": "LastName",
    ///        "organisationId": "CcsOrgId1",
    ///        "userName": "user@mail.com",
    ///        "detail": {
    ///           "id": 1,
    ///           "groupIds": { 1, 2},
    ///           "serviceRoleGroupIds": { 1, 2 },
    ///           "identityProviderId": 1,
    ///        }
    ///     }
    ///
    /// </remarks>
    [HttpPut("v1")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task<UserEditResponseInfo> UpdateUserV1([FromQuery(Name = "user-id")] string userId, UserProfileServiceRoleGroupEditRequestInfo userProfileServiceRoleGroupEditRequestInfo)
    {
      return await _userProfileService.UpdateUserV1Async(userId, userProfileServiceRoleGroupEditRequestInfo);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, ERROR_CANNOT_DELETE_LAST_ADMIN_OF_ORGANISATION
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /users/v1?user-id=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpDelete("v1")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUserV1([FromQuery(Name = "user-id")] string userId)
    {
      await _userProfileService.DeleteUserAsync(userId);
    }

    #endregion


    /// <summary>
    /// Verify user details for joining org request
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_DETAIL
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /users/join-request-validation?details=encrypted-token
    ///     
    /// </remarks>
    [HttpGet("join-request-validation")]
    [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(OrganisationJoinRequest), 200)]
    public async Task<OrganisationJoinRequest> GetUserJoinRequestDetails([FromQuery(Name = "details")] string joiningDetailsToken)
    {
      return await _userProfileService.GetUserJoinRequestDetails(joiningDetailsToken);
    }
  }
}
