using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Api.Controllers
{
  [Route("contact")]
  [ApiController]
  public class ContactController : ControllerBase
  {

    private readonly IContactService _contactService;
    public ContactController(IContactService contactService)
    {
      _contactService = contactService;
    }

    /// <summary>
    /// Method to delete a contact.
    /// </summary>
    /// <response  code="200">Successfully deleted</response>
    /// <response  code="401">Authentication fails</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /contact/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{id}")]
    [SwaggerOperation(Tags = new[] { "contact" })]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    public async Task Delete(int id)
    {
      await _contactService.DeleteAsync(id);
    }

    /// <summary>
    /// Method to get list of contacts.
    /// </summary>
    /// <response  code="200">Get list of contact details</response>
    /// <response  code="401">Authentication fails</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /contact?organisationId=1
    ///     
    ///
    /// </remarks>
    [HttpGet]
    [SwaggerOperation(Tags = new[] { "contact" })]
    [ProducesResponseType(typeof(List<ContactDetailDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<List<ContactDetailDto>> Get([FromQuery] ContactRequestFilter contactRequestFilter)
    {
      return await _contactService.GetAsync(contactRequestFilter);
    }

    /// <summary>
    /// Method to get a contact by contact id.
    /// </summary>
    /// <response  code="200">Get the contact details</response>
    /// <response  code="204">No content</response>
    /// <response  code="401">Authentication fails</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /contact/1
    ///     
    ///
    /// </remarks>
    [HttpGet("{contactId}")]
    [SwaggerOperation(Tags = new[] { "contact" })]
    [ProducesResponseType(typeof(ContactDetailDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<ContactDetailDto> Get(int contactId)
    {
      return await _contactService.GetAsync(contactId);
    }

    /// <summary>
    /// Method to create any contact type. Contact Type is required.
    /// Contact types:- 0 - Organisation, 1 - OrganisationPerson, 2 - User
    /// When creating the organisation person contact or organisation contact, organisation id is required.
    /// </summary>
    /// <response  code="200">Contact Id</response>
    /// <response  code="401">Authentication fails</response>
    /// <response  code="400">Invalid contact type.
    /// Code: INVALID_CONTACT_TYPE (Contact type should be 0 - Organisation, 1 - OrganisationPerson, 2 - User)
    /// Code: ERROR_NAME_REQUIRED (name is required, when contact type is OrganisationPerson or User),
    /// Code: ERROR_EMAIL_REQUIRED (email is required, when contact type is OrganisationPerson or User)
    /// Code: ERROR_EMAIL_FORMAT (invaid email, when contact type is OrganisationPerson or User)
    /// Code: ERROR_PHONE_NUMBER_REQUIRED (phone number is required, when contact type is OrganisationPerson or User)
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /contact
    ///     {
    ///        "organisationId": 1,
    ///        "name": "Test User",
    ///        "email": "testuser@xxx.com",
    ///        "teamName": "teamname",
    ///        "phoneNumber": "0123456789",
    ///        "contactType": 0, (0 - Organisation, 1 - OrganisationPerson, 2 - User)
    ///        "address": {
    ///                     "streetAddress": "streetAddress",
    ///                     "locality": "locality",
    ///                     "region": "region",
    ///                     "postalCode": "postalCode",
    ///                     "countryCode": "countryCode",
    ///                     "uprn": "uprn"
    ///                   }
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Tags = new[] { "contact" })]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<int> Post(ContactDetailDto contactDetailDto)
    {
      return await _contactService.CreateAsync(contactDetailDto);
    }

    /// <summary>
    /// Method to update any contact type. Contact Type is required.
    /// Contact types:- 0 - Organisation, 1 - OrganisationPerson, 2 - User
    /// </summary>
    /// <response  code="200">Contact Id</response>
    /// <response  code="401">Authentication fails</response>
    /// <response  code="400">Invalid contact type.
    /// Code: INVALID_CONTACT_TYPE (Contact type should be 0 - Organisation, 1 - OrganisationPerson, 2 - User)
    /// Code: ERROR_NAME_REQUIRED (name is required, when contact type is OrganisationPerson or User),
    /// Code: ERROR_EMAIL_REQUIRED (email is required, when contact type is OrganisationPerson or User)
    /// Code: ERROR_EMAIL_FORMAT (invaid email, when contact type is OrganisationPerson or User)
    /// Code: ERROR_PHONE_NUMBER_REQUIRED (phone number is required, when contact type is OrganisationPerson or User)
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /contact/1
    ///     {
    ///        "organisationId": 1,
    ///        "name": "Test User",
    ///        "email": "testuser@xxx.com",
    ///        "teamName": "teamname",
    ///        "phoneNumber": "0123456789",
    ///        "contactType": 0, (0 - Organisation, 1 - OrganisationPerson, 2 - User)
    ///        "address": {
    ///                     "streetAddress": "streetAddress",
    ///                     "locality": "locality",
    ///                     "region": "region",
    ///                     "postalCode": "postalCode",
    ///                     "countryCode": "countryCode",
    ///                     "uprn": "uprn"
    ///                   }
    ///     }
    ///
    /// </remarks>
    [HttpPut("{contactId}")]
    [SwaggerOperation(Tags = new[] { "contact" })]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<int> Put([FromRoute] int contactId, [FromBody] ContactDetailDto contactDetailDto)
    {
      return await _contactService.UpdateAsync(contactId, contactDetailDto);
    }
  }
}
