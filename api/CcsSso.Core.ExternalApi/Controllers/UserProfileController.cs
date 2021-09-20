using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ExternalApi.Authorisation;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
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
    public UserProfileController(IUserContactService contactService, IUserProfileService userProfileService)
    {
      _contactService = contactService;
      _userProfileService = userProfileService;
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
    /// Error Codes: INVALID_USER_ID, INVALID_FIRST_NAME, INVALID_LAST_NAME, INVALID_USER_GROUP_ROLE, INVALID_USER_GROUP, INVALID_ROLE, INVALID_IDENTITY_PROVIDER, INVALID_USER_DETAIL
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /users
    ///     {
    ///        "firstName": "FirstName",
    ///        "lastName": "LastName",
    ///        "userName": "user@mail.com",
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
    ///     GET /users?userId=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpGet]
    [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserProfileResponseInfo), 200)]
    public async Task<UserProfileResponseInfo> GetUser(string userId)
    {
      return await _userProfileService.GetUserAsync(userId);
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
    ///     PUT /users?userId=user@mail.com
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
    public async Task<UserEditResponseInfo> UpdateUser(string userId, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      return await _userProfileService.UpdateUserAsync(userId, userProfileRequestInfo);
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
    ///     DELETE /users?userId=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpDelete]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUser(string userId)
    {
      await _userProfileService.DeleteUserAsync(userId);
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpPut("reset-password")]
    [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task ResetUserPassword(string userId, string? component)
    {
      await _userProfileService.ResetUserPasswodAsync(userId, component);
    }

    [HttpPut("remove-admin-roles")]
    [ClaimAuthorise("ORG_USER_SUPPORT")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task RemoveAdminRoles(string userId)
    {
      await _userProfileService.RemoveAdminRolesAsync(userId);
    }

    [HttpPut("add-admin-role")]
    [ClaimAuthorise("ORG_USER_SUPPORT")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task AddAdminRole(string userId)
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
    ///     POST /users/contacts?userId=user@mail.com
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
    public async Task<int> CreateUserContact(string userId, ContactRequestInfo contactInfo)
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
    ///     GET /user-profile/users/contacts?userId=user@mail.com
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(UserContactInfoList), 200)]
    public async Task<UserContactInfoList> GetUserContactsList(string userId, [FromQuery] string contactType)
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
    ///     GET /users/contacts/1?userId=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpGet("contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(UserContactInfo), 200)]
    public async Task<UserContactInfo> GetUserContact(string userId, int contactId)
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
    ///     PUT /users/contacts/1?userId=user@mail.com
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
    public async Task UpdateUserContact(string userId, int contactId, ContactRequestInfo contactInfo)
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
    ///     DELETE /users/contacts/1?userId=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpDelete("contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("USER")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUserContact(string userId, int contactId)
    {
      await _contactService.DeleteUserContactAsync(userId, contactId);
    }
    #endregion
  }
}
