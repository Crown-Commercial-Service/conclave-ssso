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
        ///     GET /users?user-id=user@mail.com
        ///     
        ///
        /// </remarks>
        [HttpGet]
        [ClaimAuthorise("ORG_USER_SUPPORT", "ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
        [OrganisationAuthorise("USER")]
        [SwaggerOperation(Tags = new[] { "User" })]
        [ProducesResponseType(typeof(UserProfileResponseInfo), 200)]
        public async Task<UserProfileResponseInfo> GetUser([FromQuery(Name = "user-id")] string userId, [FromQuery] string delegatedOrgId = "")
        {
            return await _userProfileService.GetUserAsync(userId, delegatedOrgId);
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

        #region Delegated access
        /// <summary>
        /// Allows admin to delegate other org user to represent org
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
        ///     POST /delegate-user
        ///     {
        ///       "userName": "brijrajsinh999@yopmail.com",
        ///       "detail": {
        ///         "delegatedOrgId": "994051658826844094",
        ///         "roleIds": [
        ///           1
        ///         ],
        ///         "startDate": "2022-08-05T08:11:19.467Z",
        ///         "endDate": "2023-08-05T08:11:19.467Z"
        ///       }
        ///     }
        ///     
        /// </remarks>
        [HttpPost("delegate-user")]
        [ClaimAuthorise("ORG_ADMINISTRATOR")]
        [OrganisationAuthorise("USER_POST")]
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
        /// Error Codes: INVALID_USER_ID, INVALID_ROLE, INVALID_USER_DETAIL
        /// </response>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT /delegate-user
        ///     {
        ///       "userName": "brijrajsinh999@yopmail.com",
        ///       "detail": {
        ///         "delegatedOrgId": "994051658826844094",
        ///         "roleIds": [
        ///           1,2
        ///         ],
        ///         "startDate": "2022-08-05T08:11:19.467Z",
        ///         "endDate": "2023-06-05T08:11:19.467Z"
        ///       }
        ///     }
        ///
        /// </remarks>
        [HttpPut("delegate-user")]
        [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
        [OrganisationAuthorise("USER")]
        [SwaggerOperation(Tags = new[] { "User" })]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task UpdateDelegatedUser(DelegatedUserProfileRequestInfo userProfileRequestInfo)
        {
            await _userProfileService.UpdateDelegatedUserAsync(userProfileRequestInfo);
        }

        /// <summary>
        /// Allows admin to remove/revoke user delegation for organization
        /// </summary>
        /// <response  code="200">Ok</response>
        /// <response  code="401">Unauthorised</response>
        /// <response  code="403">Forbidden</response>
        /// <response  code="404">Not found</response>
        /// <response  code="400">Bad request.
        /// Error Codes: INVALID_USER_ID, ERROR_ORGANISATION_ID_REQUIRED
        /// </response>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE /delegate-user?user-id=user@mail.com&organisationId=994051658826844094
        ///     
        ///
        /// </remarks>
        [HttpDelete("delegate-user")]
        [ClaimAuthorise("ORG_ADMINISTRATOR")]
        [OrganisationAuthorise("USER")]
        [SwaggerOperation(Tags = new[] { "User" })]
        [ProducesResponseType(typeof(void), 200)]
        public async Task DeleteDelegatedUser([FromQuery(Name = "user-id")] string userId, [FromQuery] string organisationId)
        {
            await _userProfileService.RemoveDelegatedAccessForUserAsync(userId, organisationId);
        }
        #endregion

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
    }
}
