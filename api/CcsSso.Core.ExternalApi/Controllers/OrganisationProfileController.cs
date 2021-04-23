using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.ExternalApi.Controllers
{
  [Route("organisations")]
  [ApiController]
  public class OrganisationProfileController : ControllerBase
  {
    private readonly IOrganisationProfileService _organisationService;
    private readonly IOrganisationContactService _contactService;
    private readonly IOrganisationSiteService _siteService;
    private readonly IOrganisationSiteContactService _siteContactService;
    private readonly IUserProfileService _userProfileService;
    private readonly IOrganisationGroupService _organisationGroupService;

    public OrganisationProfileController(IOrganisationProfileService organisationService, IOrganisationContactService contactService,
       IOrganisationSiteService siteService, IOrganisationSiteContactService siteContactService, IUserProfileService userProfileService,
       IOrganisationGroupService organisationGroupService)
    {
      _organisationService = organisationService;
      _contactService = contactService;
      _siteService = siteService;
      _siteContactService = siteContactService;
      _userProfileService = userProfileService;
      _organisationGroupService = organisationGroupService;
    }

    #region Organisation profile

    /// <summary>
    /// Allows a user to create organisation
    /// </summary>
    /// <response  code="200">Ok. Return created organisation id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="400">Bad request.
    /// Error Codes:  INVALID_IDENTIFIER, INVALID_LEGAL_NAME, INVALID_URI, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/
    ///     {
    ///       "Identifier": {
    ///         "legalName": "Kier Limited",
    ///         "uri": "http://data.companieshouse.gov.uk/doc/company/1"
    ///       },
    ///       "address": {
    ///         "streetAddress": "1600 Amphitheatre Pkwy",
    ///         "locality": "Mountain View.",
    ///         "region": "CA.",
    ///         "postalCode": "94043",
    ///         "countryCode": "UK"
    ///       },
    ///       "Detail": {
    ///         "organisationId": "CiiOrgidId",
    ///         "rightToBuy": "true",
    ///         "is_sme": 1,
    ///         "is_vcse": 1,
    ///         "active": 1
    ///       }
    ///     }
    ///     
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<string> CreateOrganisation(OrganisationProfileInfo organisationProfileInfo)
    {
      return await _organisationService.CreateOrganisationAsync(organisationProfileInfo);
    }

    /// <summary>
    /// Get organisation profile details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/CiiOrgidId
    ///     
    /// </remarks>
    [HttpGet("{organisationId}")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(OrganisationProfileResponseInfo), 200)]
    public async Task<OrganisationProfileResponseInfo> GetOrganisation(string organisationId)
    {
      return await _organisationService.GetOrganisationAsync(organisationId);
    }

    /// <summary>
    /// Allows a user to update organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_IDENTIFIER, INVALID_LEGAL_NAME, INVALID_URI, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /organisations/CiiOrgidId
    ///     {
    ///       "Identifier": {
    ///         "legalName": "Kier Limited",
    ///         "uri": "http://data.companieshouse.gov.uk/doc/company/1"
    ///       },
    ///       "address": {
    ///         "streetAddress": "1600 Amphitheatre Pkwy",
    ///         "locality": "Mountain View.",
    ///         "region": "CA.",
    ///         "postalCode": "94043",
    ///         "countryCode": "UK"
    ///       },
    ///       "Detail": {
    ///         "organisationId": "CiiOrgidId",
    ///         "rightToBuy": "true",
    ///         "is_sme": 1,
    ///         "is_vcse": 1,
    ///         "active": 1
    ///       }
    ///     }
    ///     
    /// </remarks>
    [HttpPut("{organisationId}")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisation(string organisationId, OrganisationProfileInfo organisationProfileInfo)
    {
      await _organisationService.UpdateOrganisationAsync(organisationId, organisationProfileInfo);
    }

    #endregion

    #region Organisation Contacts

    /// <summary>
    /// Allows a user to create organisation contact
    /// </summary>
    /// <response  code="200">Ok. Return created contact id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_EMAIL, INVALID_PHONE_NUMBER, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/contact
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
    [HttpPost("{organisationId}/contacts")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateOrganisationContact(string organisationId, ContactInfo contactInfo)
    {
      return await _contactService.CreateOrganisationContactAsync(organisationId, contactInfo);
    }

    /// <summary>
    /// Allows a user to get organisation contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET organisations/1/contact
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/contacts")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(OrganisationContactInfoList), 200)]
    public async Task<OrganisationContactInfoList> GetOrganisationContactsList(string organisationId, [FromQuery] string contactType)
    {
      return await _contactService.GetOrganisationContactsListAsync(organisationId, contactType);
    }

    /// <summary>
    /// Allows a user to retrieve details for a given organisation contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/contact/1
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/contacts/{contactId}")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(OrganisationContactInfo), 200)]
    public async Task<OrganisationContactInfo> GetOrganisationContact(string organisationId, int contactId)
    {
      return await _contactService.GetOrganisationContactAsync(organisationId, contactId);
    }

    /// <summary>
    /// Allows a user to edit organisation contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_EMAIL, INVALID_PHONE_NUMBER, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /organisations/1/contact/1
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
    [HttpPut("{organisationId}/contacts/{contactId}")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisationContact(string organisationId, int contactId, ContactInfo contactInfo)
    {
      await _contactService.UpdateOrganisationContactAsync(organisationId, contactId, contactInfo);
    }

    /// <summary>
    /// Remove a contact from an organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/contact/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{organisationId}/contacts/{contactId}")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteOrganisationContact(string organisationId, int contactId)
    {
      await _contactService.DeleteOrganisationContactAsync(organisationId, contactId);
    }

    #endregion

    #region Organisation Site

    /// <summary>
    /// Allows a user to create organisation site
    /// </summary>
    /// <response  code="200">Ok. Return created site id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_SITE_NAME, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/site
    ///     {
    ///       "siteName": "Main Branch",
    ///       "address": {
    ///         "streetAddress": "1600 Amphitheatre Pkwy",
    ///         "locality": "Mountain View.",
    ///         "region": "CA.",
    ///         "postalCode": "94043",
    ///         "countryCode": "UK"
    ///       }
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPost("{organisationId}/sites")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateOrganisationSite(string organisationId, OrganisationSiteInfo organisationSiteInfo)
    {
      return await _siteService.CreateSiteAsync(organisationId, organisationSiteInfo);
    }

    /// <summary>
    /// Allows a user to get all the sites in an organisation
    /// </summary>
    /// <response  code="200">Ok with site list</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/site
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(OrganisationSiteInfoList), 200)]
    public async Task<OrganisationSiteInfoList> GetOrganisationSite(string organisationId)
    {
      return await _siteService.GetOrganisationSitesAsync(organisationId);
    }

    /// <summary>
    /// Allows a user to get organisation site details
    /// </summary>
    /// <response  code="200">Ok with site details</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/site/1    
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites/{siteId}")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(OrganisationSiteResponse), 200)]
    public async Task<OrganisationSiteResponse> GetOrganisationSite(string organisationId, int siteId)
    {
      return await _siteService.GetSiteAsync(organisationId, siteId);
    }


    /// <summary>
    /// Allows a user to update organisation site
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_SITE_NAME, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /organisations/1/site/1
    ///     {
    ///       "siteName": "Main Branch",
    ///       "address": {
    ///         "streetAddress": "1600 Amphitheatre Pkwy",
    ///         "locality": "Mountain View.",
    ///         "region": "CA.",
    ///         "postalCode": "94043",
    ///         "countryCode": "UK"
    ///       }
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPut("{organisationId}/sites/{siteId}")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisationSite(string organisationId, int siteId, OrganisationSiteInfo organisationSiteInfo)
    {
      await _siteService.UpdateSiteAsync(organisationId, siteId, organisationSiteInfo);
    }

    /// <summary>
    /// Allows a user to delete organisation site
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/site/1    
    ///
    /// </remarks>
    [HttpDelete("{organisationId}/sites/{siteId}")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteOrganisationSite(string organisationId, int siteId)
    {
      await _siteService.DeleteSiteAsync(organisationId, siteId);
    }

    #endregion

    #region Organisation Site Contacts

    /// <summary>
    /// Allows a user to create organisation site contact
    /// </summary>
    /// <response  code="200">Ok. Return created site contact id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_EMAIL, INVALID_PHONE_NUMBER, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/site/1/contact
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
    /// </remarks>s
    [HttpPost("{organisationId}/sites/{siteId}/contacts")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateOrganisationSiteContact(string organisationId, int siteId, ContactInfo contactInfo)
    {
      return await _siteContactService.CreateOrganisationSiteContactAsync(organisationId, siteId, contactInfo);
    }

    /// <summary>
    /// Allows a user to get list of contacts for organisation site 
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET organisations/1/site/1/contact
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites/{siteId}/contacts")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(OrganisationSiteContactInfoList), 200)]
    public async Task<OrganisationSiteContactInfoList> GetOrganisationSiteContactsList(string organisationId, int siteId, string contactType)
    {
      return await _siteContactService.GetOrganisationSiteContactsListAsync(organisationId, siteId, contactType);
    }

    /// <summary>
    /// Allows a user to retrieve details for a given organisation site contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/site/1/contact/1
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites/{siteId}/contacts/{contactId}")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(OrganisationSiteContactInfo), 200)]
    public async Task<OrganisationSiteContactInfo> GetOrganisationSiteContact(string organisationId, int siteId, int contactId)
    {
      return await _siteContactService.GetOrganisationSiteContactAsync(organisationId, siteId, contactId);
    }

    /// <summary>
    /// Allows a user to edit organisation site contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_EMAIL, INVALID_PHONE_NUMBER, INSUFFICIENT_DETAILS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /organisations/1/site/1/contact/1
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
    [HttpPut("{organisationId}/sites/{siteId}/contacts/{contactId}")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisationSiteContact(string organisationId, int siteId, int contactId, ContactInfo contactInfo)
    {
      await _siteContactService.UpdateOrganisationSiteContactAsync(organisationId, siteId, contactId, contactInfo);
    }

    /// <summary>
    /// Remove a contact from an organisation site
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/site/1/contact/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{organisationId}/sites/{siteId}/contacts/{contactId}")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteOrganisationSiteContact(string organisationId, int siteId, int contactId)
    {
      await _siteContactService.DeleteOrganisationSiteContactAsync(organisationId, siteId, contactId);
    }

    #endregion

    #region Organisation Users

    /// <summary>
    /// Allows a user to retrieve users for a given organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1?userId=user@mail.com,pageSize=10,currentPage=1
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/user")]
    [SwaggerOperation(Tags = new[] { "Organisation User" })]
    [ProducesResponseType(typeof(UserListResponse), 200)]
    public async Task<UserListResponse> GetUsers(string organisationId, [FromQuery] ResultSetCriteria resultSetCriteria, string searchString)
    {
      resultSetCriteria ??= new ResultSetCriteria
      {
        CurrentPage = 1,
        PageSize = 10
      };
      resultSetCriteria.CurrentPage = resultSetCriteria.CurrentPage <= 0 ? 1 : resultSetCriteria.CurrentPage;
      resultSetCriteria.PageSize = resultSetCriteria.PageSize <= 0 ? 10 : resultSetCriteria.PageSize;
      return await _userProfileService.GetUsersAsync(organisationId, resultSetCriteria, searchString);
    }
    #endregion

    #region Organisation Group
    /// <summary>
    /// Create organisation group
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <response  code="409">Resource already exists</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_GROUP_NAME
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/groups
    ///     {
    ///       'groupName': "Group Name"
    ///     }
    ///     
    /// </remarks>
    [HttpPost("{organisationId}/groups")]
    [SwaggerOperation(Tags = new[] { "Organisation Group" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateOrganisationGroup(string organisationId, OrganisationGroupNameInfo organisationGroupNameInfo)
    {
      return await _organisationGroupService.CreateGroupAsync(organisationId, organisationGroupNameInfo);
    }

    /// <summary>
    /// Delete organisation group
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/groups/1
    ///     
    /// </remarks>
    [HttpDelete("{organisationId}/groups/{groupId}")]
    [SwaggerOperation(Tags = new[] { "Organisation Group" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteOrganisationGroup(string organisationId, int groupId)
    {
      await _organisationGroupService.DeleteGroupAsync(organisationId, groupId);
    }

    /// <summary>
    /// Get organisation group
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/groups/1
    ///     
    /// </remarks>
    [HttpGet("{organisationId}/groups/{groupId}")]
    [SwaggerOperation(Tags = new[] { "Organisation Group" })]
    [ProducesResponseType(typeof(OrganisationGroupResponseInfo), 200)]
    public async Task<OrganisationGroupResponseInfo> GetOrganisationGroup(string organisationId, int groupId)
    {
      return await _organisationGroupService.GetGroupAsync(organisationId, groupId);
    }

    /// <summary>
    /// Get organisation groups
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/groups
    ///     
    /// </remarks>
    [HttpGet("{organisationId}/groups")]
    [SwaggerOperation(Tags = new[] { "Organisation Group" })]
    [ProducesResponseType(typeof(OrganisationGroupList), 200)]
    public async Task<OrganisationGroupList> GetOrganisationGroups(string organisationId, string searchString = null)
    {
      return await _organisationGroupService.GetGroupsAsync(organisationId, searchString);
    }

    /// <summary>
    /// Update organisation group
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <response  code="409">Resource already exists</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_ROLE_INFO, INVALID_USER_INFO
    /// </response>
    /// <remarks>
    /// Sample requests:
    ///
    ///     PATCH /organisations/1/groups/1
    ///     {
    ///       'groupName': "Group Name",
    ///       'roleInfo': null,
    ///       'userInfo': null
    ///     }
    ///
    ///     PATCH /organisations/1/groups/1
    ///     {
    ///       'groupName': "",
    ///       'roleInfo':{
    ///           'addedRoleIds': [ 1, 2 ],
    ///           'removedRoleIds': [ 3 ]
    ///        },
    ///       'userInfo':{
    ///           'addedUserIds': [ "user1@mail.com", "user2@mail.com" ],
    ///           'addedUserIds': [ "user3@mail.com" ]
    ///        }
    ///     }
    ///
    ///     PATCH /organisations/1/groups/1
    ///     {
    ///       'groupName': null,
    ///       'roleInfo':{
    ///           'addedRoleIds': [ 1, 2 ],
    ///           'removedRoleIds': [ 3 ]
    ///        },
    ///       'userInfo':{
    ///           'addedUserIds': [ "user1@mail.com", "user2@mail.com" ],
    ///           'addedUserIds': [ "user3@mail.com" ]
    ///        }
    ///     }
    ///
    ///     PATCH /organisations/1/groups/1
    ///     {
    ///       'groupName': "Group Name",
    ///       'roleInfo': null,
    ///       'userInfo':{
    ///           'addedUserIds': [ "user1@mail.com", "user2@mail.com" ],
    ///           'addedUserIds': [ "user3@mail.com" ]
    ///        }
    ///     }
    ///
    ///     PATCH /organisations/1/groups/1
    ///     {
    ///       'groupName': "Group Name",
    ///       'roleInfo':{
    ///           'addedRoleIds': [ 1, 2 ],
    ///           'removedRoleIds': [ 3 ]
    ///        },
    ///       'userInfo': null
    ///     }
    ///     
    /// </remarks>
    [HttpPatch("{organisationId}/groups/{groupId}")]
    [SwaggerOperation(Tags = new[] { "Organisation Group" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisationGroup(string organisationId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo)
    {
      await _organisationGroupService.UpdateGroupAsync(organisationId, groupId, organisationGroupRequestInfo);
    }
    #endregion

    #region Organisation IdentityProviders
    /// <summary>
    /// Allows a user to get identity provider details of an organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET organisations/1/identity-providers
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/identity-providers")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(List<IdentityProviderDetail>), 200)]
    public async Task<List<IdentityProviderDetail>> GetIdentityProviders(string organisationId)
    {
      return await _organisationService.GetOrganisationIdentityProvidersAsync(organisationId);
    }

    [HttpPut("{organisationId}/identity-providers/update")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(List<IdentityProviderDetail>), 200)]
    public async Task UpdateIdentityProvider(string organisationId, [System.Web.Http.FromUri] string idpName, [System.Web.Http.FromUri] bool enabled)
    {
      await _organisationService.UpdateIdentityProviderAsync(organisationId, idpName, enabled);
    }

    #endregion

    #region Organisation Role
    /// <summary>
    /// Get organisation roles
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/roles
    ///     
    /// </remarks>
    [HttpGet("{organisationId}/roles")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(List<OrganisationRole>), 200)]
    public async Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId)
    {
      return await _organisationService.GetOrganisationRolesAsync(organisationId);
    }

    [HttpGet("{ciiOrganisationId}/eligableRoles")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(List<OrganisationRole>), 200)]
    public async Task<List<OrganisationRole>> GetEligableRoles(string ciiOrganisationId)
    {
      return await _organisationService.GetEligableRolesAsync(ciiOrganisationId);
    }

    [HttpPut("{ciiOrganisationId}/updateEligableRoles")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task updateEligableRoles(string ciiOrganisationId, OrganisationRoleUpdate model)
    {
      await _organisationService.UpdateOrganisationAsync(ciiOrganisationId, model.IsBuyer, model.RolesToAdd, model.RolesToDelete);
    }

    #endregion

  }
}
