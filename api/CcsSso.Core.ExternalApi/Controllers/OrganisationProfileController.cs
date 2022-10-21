using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ExternalApi.Authorisation;
using CcsSso.Domain.Constants;
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
    private readonly IOrganisationAuditEventService _organisationAuditEventService;

    public OrganisationProfileController(IOrganisationProfileService organisationService, IOrganisationContactService contactService,
       IOrganisationSiteService siteService, IOrganisationSiteContactService siteContactService, IUserProfileService userProfileService,
       IOrganisationGroupService organisationGroupService, IOrganisationAuditEventService organisationAuditEventService)
    {
      _organisationService = organisationService;
      _contactService = contactService;
      _siteService = siteService;
      _siteContactService = siteContactService;
      _userProfileService = userProfileService;
      _organisationGroupService = organisationGroupService;
      _organisationAuditEventService = organisationAuditEventService;
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
    [ClaimAuthorise("ORG_ADMINISTRATOR", "MANAGE_SUBSCRIPTIONS")]
    [OrganisationAuthorise("ORGANISATION")]
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
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisation(string organisationId, OrganisationProfileInfo organisationProfileInfo)
    {
      await _organisationService.UpdateOrganisationAsync(organisationId, organisationProfileInfo);
    }

    // # Auto validation
    /// <summary>
    /// Organisation auto validation for verified buyer
    /// </summary>
    /// <response  code="200">Ok. Return true if auto validation passed else return false</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="400">Bad request.
    /// Error Codes:  INVALID_CII_ORGANISATION_ID
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/auto-validate
    ///     {
    ///       "AdminEmailId" : "user@mail.com",
    ///       "IsFromBackgroundJob" : false
    ///     }
    ///     
    /// </remarks>
    [HttpPost("{ciiOrganisationId}/auto-validate")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Auto Validation" })]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<bool> AutoValidateOrganisation(string ciiOrganisationId, AutoValidationDetails autoValidationDetails)
    {
      return await _organisationService.AutoValidateOrganisation(ciiOrganisationId, autoValidationDetails.AdminEmailId, autoValidationDetails.IsFromBackgroundJob);
    }
    #endregion

    #region Organisation Contacts

    /// <summary>
    /// Allows a user to assign user/site contacts for an organisation
    /// </summary>
    /// <response  code="200">Ok. Return assigned site contact ids</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: ERROR_INVALID_ASSIGNING_CONTACT_POINT_IDS, ERROR_INVALID_CONTACT_ASSIGNEMNT_TYPE, ERROR_INVALID_USER_ID_FOR_CONTACT_ASSIGNEMNT,
    /// ERROR_INVALID_SITE_ID_FOR_CONTACT_ASSIGNEMNT, ERROR_DUPLICATE_CONTACT_ASSIGNMENT
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/assigned-contacts
    ///     {
    ///        "AssigningContactType": 1, (User:1, Site:2)
    ///        "AssigningContactPointIds": [1, 2],
    ///        "AssigningContactsUserId": "user@mail.com"
    ///     }
    ///
    ///     POST /organisations/1/contacts/assign
    ///     {
    ///        "AssigningContactType": 2, (User:1, Site:2)
    ///        "AssigningContactPointIds": [1, 2],
    ///        "AssigningContactsSiteId": 1
    ///     }
    ///     
    ///
    /// </remarks>s
    [HttpPost("{organisationId}/assigned-contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(List<int>), 200)]
    public async Task<List<int>> AssignContactsToOrganisationSite(string organisationId, ContactAssignmentInfo contactAssignmentInfo)
    {
      return await _contactService.AssignContactsToOrganisationAsync(organisationId, contactAssignmentInfo);
    }

    /// <summary>
    /// Allows a user to unassign contacts from an organisation.
    /// Should provide the assigned contacts contactpoint ids as a query parameter list
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: ERROR_INVALID_UNASSIGNING_CONTACT_POINT_IDS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/assigned-contacts?contact-point-ids=2
    ///     
    ///
    /// </remarks>s
    [HttpDelete("{organisationId}/assigned-contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UnAssignContactsFromOrganisationSite(string organisationId, [FromQuery(Name = "contact-point-ids")] List<int> contactPointIds)
    {
      await _contactService.UnassignOrganisationContactsAsync(organisationId, contactPointIds);
    }

    /// <summary>
    /// Allows a user to create organisation contact
    /// </summary>
    /// <response  code="200">Ok. Return created contact id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INSUFFICIENT_DETAILS, INVALID_CONTACT_TYPE, INVALID_EMAIL, INVALID_PHONE_NUMBER
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/contacts
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
    [HttpPost("{organisationId}/contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateOrganisationContact(string organisationId, ContactRequestInfo contactInfo)
    {
      return await _contactService.CreateOrganisationContactAsync(organisationId, contactInfo);
    }

    /// <summary>
    /// Allows a user to get organisation contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET organisations/1/contacts
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(OrganisationContactInfoList), 200)]
    public async Task<OrganisationContactInfoList> GetOrganisationContactsList(string organisationId, [FromQuery(Name = "contact-type")] string contactType, [FromQuery(Name = "contact-assigned-status")] ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All)
    {
      return await _contactService.GetOrganisationContactsListAsync(organisationId, contactType, contactAssignedStatus);
    }

    /// <summary>
    /// Allows a user to retrieve details for a given organisation contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/contacts/1
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INSUFFICIENT_DETAILS, INVALID_CONTACT_TYPE, INVALID_EMAIL, INVALID_PHONE_NUMBER
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /organisations/1/contacts/1
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
    [HttpPut("{organisationId}/contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisationContact(string organisationId, int contactId, ContactRequestInfo contactInfo)
    {
      await _contactService.UpdateOrganisationContactAsync(organisationId, contactId, contactInfo);
    }

    /// <summary>
    /// Remove a contact from an organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/contacts/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{organisationId}/contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// <response  code="409">Resource already exists</response>
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
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/site?search-string=sitename
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(OrganisationSiteInfoList), 200)]
    public async Task<OrganisationSiteInfoList> GetOrganisationSites(string organisationId, [FromQuery(Name = "search-string")] string searchString = null)
    {
      return await _siteService.GetOrganisationSitesAsync(organisationId, searchString);
    }

    /// <summary>
    /// Allows a user to get organisation site details
    /// </summary>
    /// <response  code="200">Ok with site details</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/site/1    
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites/{siteId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// <response  code="409">Resource already exists</response>
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
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/site/1    
    ///
    /// </remarks>
    [HttpDelete("{organisationId}/sites/{siteId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteOrganisationSite(string organisationId, int siteId)
    {
      await _siteService.DeleteSiteAsync(organisationId, siteId);
    }

    #endregion

    #region Organisation Site Contacts

    /// <summary>
    /// Allows a user to assign user contacts for an organisation site
    /// </summary>
    /// <response  code="200">Ok. Return assigned site contact ids</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: ERROR_INVALID_ASSIGNING_CONTACT_POINT_IDS, ERROR_INVALID_CONTACT_ASSIGNEMNT_TYPE, ERROR_INVALID_USER_ID_FOR_CONTACT_ASSIGNEMNT, ERROR_DUPLICATE_CONTACT_ASSIGNMENT
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/sites/1/assigned-contacts
    ///     {
    ///        "AssigningContactType": 1, (User:1, Site:2 Only user contacts are valid here)
    ///        "AssigningContactPointIds": [1, 2],
    ///        "AssigningContactsUserId": "user@mail.com"
    ///     }
    ///     
    ///
    /// </remarks>s
    [HttpPost("{organisationId}/sites/{siteId}/assigned-contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(List<int>), 200)]
    public async Task<List<int>> AssignContactsToOrganisationSite(string organisationId, int siteId, ContactAssignmentInfo contactAssignmentInfo)
    {
      return await _siteContactService.AssignContactsToSiteAsync(organisationId, siteId, contactAssignmentInfo);
    }


    /// <summary>
    /// Allows a user to unassign contacts from an organisation site.
    /// Should provide the assigned contacts contactpoint ids as a query parameter list
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: ERROR_INVALID_UNASSIGNING_CONTACT_POINT_IDS
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/sites/1/assigned-contacts?contactPointIds=2
    ///     
    ///
    /// </remarks>s
    [HttpDelete("{organisationId}/sites/{siteId}/assigned-contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UnAssignContactsFromOrganisationSite(string organisationId, int siteId, [FromQuery(Name = "contact-point-ids")] List<int> contactPointIds)
    {
      await _siteContactService.UnassignSiteContactsAsync(organisationId, siteId, contactPointIds);
    }

    /// <summary>
    /// Allows a user to create organisation site contact
    /// </summary>
    /// <response  code="200">Ok. Return created site contact id</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INSUFFICIENT_DETAILS, INVALID_CONTACT_TYPE, INVALID_EMAIL, INVALID_PHONE_NUMBER
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/1/sites/1/contacts
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
    /// </remarks>s
    [HttpPost("{organisationId}/sites/{siteId}/contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateOrganisationSiteContact(string organisationId, int siteId, ContactRequestInfo contactInfo)
    {
      return await _siteContactService.CreateOrganisationSiteContactAsync(organisationId, siteId, contactInfo);
    }

    /// <summary>
    /// Allows a user to get list of contacts for organisation site 
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET organisations/1/sites/1/contacts
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites/{siteId}/contacts")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(OrganisationSiteContactInfoList), 200)]
    public async Task<OrganisationSiteContactInfoList> GetOrganisationSiteContactsList(string organisationId, int siteId, [FromQuery(Name = "contact-type")] string contactType, [FromQuery(Name = "contact-assigned-status")] ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All)
    {
      return await _siteContactService.GetOrganisationSiteContactsListAsync(organisationId, siteId, contactType, contactAssignedStatus);
    }

    /// <summary>
    /// Allows a user to retrieve details for a given organisation site contact
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/sites/1/contacts/1
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/sites/{siteId}/contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INSUFFICIENT_DETAILS, INVALID_CONTACT_TYPE, INVALID_EMAIL, INVALID_PHONE_NUMBER
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /organisations/1/sites/1/contacts/1
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
    [HttpPut("{organisationId}/sites/{siteId}/contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation site contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateOrganisationSiteContact(string organisationId, int siteId, int contactId, ContactRequestInfo contactInfo)
    {
      await _siteContactService.UpdateOrganisationSiteContactAsync(organisationId, siteId, contactId, contactInfo);
    }

    /// <summary>
    /// Remove a contact from an organisation site
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/sites/1/contacts/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{organisationId}/sites/{siteId}/contacts/{contactId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// #Delegated
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// NOTE:- query params page-size, current-page
    /// Sample request:
    ///
    ///     GET /organisations/1/users?page-size=10,current-page=1
    ///     
    ///
    /// </remarks>
    [HttpGet("{organisationId}/users")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation User" })]
    [ProducesResponseType(typeof(UserListResponse), 200)]
    public async Task<UserListResponse> GetUsers(string organisationId, [FromQuery] ResultSetCriteria resultSetCriteria, [FromQuery] UserFilterCriteria userFilterCriteria)
    {
      resultSetCriteria ??= new ResultSetCriteria
      {
        CurrentPage = 1,
        PageSize = 10
      };
      resultSetCriteria.CurrentPage = resultSetCriteria.CurrentPage <= 0 ? 1 : resultSetCriteria.CurrentPage;
      resultSetCriteria.PageSize = resultSetCriteria.PageSize <= 0 ? 10 : resultSetCriteria.PageSize;
      return await _userProfileService.GetUsersAsync(organisationId, resultSetCriteria, userFilterCriteria);
    }
    #endregion

    #region Organisation Group
    /// <summary>
    /// Create organisation group
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
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
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisations/1/groups/1
    ///     
    /// </remarks>
    [HttpDelete("{organisationId}/groups/{groupId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/groups/1
    ///     
    /// </remarks>
    [HttpGet("{organisationId}/groups/{groupId}")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/groups
    ///     
    /// </remarks>
    [HttpGet("{organisationId}/groups")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER", "ORG_USER_SUPPORT")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation Group" })]
    [ProducesResponseType(typeof(OrganisationGroupList), 200)]
    public async Task<OrganisationGroupList> GetOrganisationGroups(string organisationId, [FromQuery(Name = "search-string")] string searchString = null)
    {
      return await _organisationGroupService.GetGroupsAsync(organisationId, searchString);
    }

    /// <summary>
    /// Update organisation group
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
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
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
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
    /// <response  code="403">Forbidden</response>
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
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(List<IdentityProviderDetail>), 200)]
    public async Task<List<IdentityProviderDetail>> GetIdentityProviders(string organisationId)
    {
      return await _organisationService.GetOrganisationIdentityProvidersAsync(organisationId);
    }

    /// <summary>
    /// Allows a user to update identity provider details of an organisation
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET organisations/1/identity-providers
    ///     {
    ///       ciiOrganisationId: "orgid",
    ///       changedOrgIdentityProviders: [
    ///         {
    ///           id: 1
    ///         }
    ///       ]
    ///      }
    ///
    /// </remarks>
    [HttpPut("{organisationId}/identity-providers")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [OrganisationAuthorise("ORGANISATION")]
    [ProducesResponseType(200)]
    public async Task UpdateIdentityProvider(OrgIdentityProviderSummary orgIdentityProviderSummary)
    {
      await _organisationService.UpdateIdentityProviderAsync(orgIdentityProviderSummary);
    }

    #endregion

    #region Organisation Role
    /// <summary>
    /// Get organisation roles
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/roles
    ///     
    /// </remarks>
    [HttpGet("{organisationId}/roles")]
    [ClaimAuthorise("MANAGE_SUBSCRIPTIONS", "ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [OrganisationAuthorise("ORGANISATION")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(List<OrganisationRole>), 200)]
    public async Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId)
    {
      return await _organisationService.GetOrganisationRolesAsync(organisationId);
    }

    /// <summary>
    /// Update organisation eligible roles
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Resource not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /organisations/1/updateEligableRoles
    ///     {
    ///       isBuyer: true,
    ///       rolesToAdd: [
    ///         {
    ///           id: 1
    ///         }
    ///       ],
    ///       rolesToDelete: [
    ///         {
    ///           id: 1
    ///         }
    ///       ]
    ///      }
    ///     
    /// </remarks>
    [HttpPut("{organisationId}/roles")]
    [ClaimAuthorise("MANAGE_SUBSCRIPTIONS")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task UpdateEligableRoles(string organisationId, OrganisationRoleUpdate model)
    {
      await _organisationService.UpdateOrganisationEligibleRolesAsync(organisationId, model.IsBuyer, model.RolesToAdd, model.RolesToDelete);
    }

    #endregion

    #region Organisation Audit Event
    /// <summary>
    /// To create organisation audit event log
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: ERROR_ORGANISATION_ID_REQUIRED
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /organisations/auditevent
    ///     [
    ///       {
    ///         "organisationId": 1,
    ///         "schemeIdentifier": "08236144",
    ///         "firstName": "",
    ///         "lastName": "",
    ///         "groupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "actioned": "",
    ///         "actionedBy": "",
    ///         "event": "",
    ///         "roles": ""
    ///       }
    ///     ]
    ///     
    ///
    /// </remarks>
    [HttpPost("auditevent")]
    [SwaggerOperation(Tags = new[] { "Organisation Audit Event" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task CreateOrganisationAuditEvent(List<OrganisationAuditEventInfo> organisationAuditEventInfoList)
    {
      await _organisationAuditEventService.CreateOrganisationAuditEventAsync(organisationAuditEventInfoList);
    }

    #endregion
  }
}
