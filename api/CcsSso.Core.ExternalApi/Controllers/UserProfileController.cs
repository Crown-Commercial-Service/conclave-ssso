using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
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
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUser(string userId)
    {
      await _userProfileService.DeleteUserAsync(userId);
    }

    [HttpPut("UpdateUserRoles")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task<UserEditResponseInfo> UpdateUserRoles(string userId, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      return await _userProfileService.UpdateUserRolesAsync(userId, userProfileRequestInfo);
    }

    [HttpPut("AddAdminRole")]
    [SwaggerOperation(Tags = new[] { "User" })]
    [ProducesResponseType(typeof(UserEditResponseInfo), 200)]
    public async Task<UserEditResponseInfo> AddAdminRole(string userId, UserProfileEditRequestInfo userProfileRequestInfo)
    {
      return await _userProfileService.AddAdminRoleAsync(userId, userProfileRequestInfo);
    }
    #endregion

    #region User contact
    /// <summary>
    /// Allows a user to create user contact details
    /// </summary>
    /// <response  code="200">Ok. Return created contact id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INSUFFICIENT_DETAILS, INVALID_EMAIL, INVALID_PHONE_NUMBER
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /users/contact?userId=user@mail.com
    ///     {
    ///        "contactReason": "BILLING/SHIPPING",
    ///        "name": "Test User",
    ///        "email": "testuser@mail.com",
    ///        "phoneNumber": "+551155256325",
    ///        "fax": "9123453",
    ///        "webUrl": "testuser.com"
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPost("contact")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateUserContact(string userId, ContactInfo contactInfo)
    {
      return await _contactService.CreateUserContactAsync(userId, contactInfo);
    }

    /// <summary>
    /// Allows a user to get user contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /user-profile/users/contact?userId=user@mail.com
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("contact")]
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
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /users/contact/1?userId=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpGet("contact/{contactId}")]
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
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID, INSUFFICIENT_DETAILS, INVALID_EMAIL, INVALID_PHONE_NUMBER
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /users/contact/1?userId=user@mail.com
    ///     {
    ///        "contactReason": "BILLING/SHIPPING",
    ///        "name": "Test User",
    ///        "email": "testuser@mail.com",
    ///        "phoneNumber": "+551155256325",
    ///        "fax": "9123453",
    ///        "webUrl": "testuser.com"
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPut("contact/{contactId}")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateUserContact(string userId, int contactId, ContactInfo contactInfo)
    {
      await _contactService.UpdateUserContactAsync(userId, contactId, contactInfo);
    }

    /// <summary>
    /// Remove a contact from user
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_USER_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /users/contact/1?userId=user@mail.com
    ///     
    ///
    /// </remarks>
    [HttpDelete("contact/{contactId}")]
    [SwaggerOperation(Tags = new[] { "User contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteUserContact(string userId, int contactId)
    {
      await _contactService.DeleteUserContactAsync(userId, contactId);
    }
    #endregion
  }
}
