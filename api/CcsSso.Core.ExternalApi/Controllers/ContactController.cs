using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.ExternalApi.Authorisation;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Controllers
{
  [Route("contacts")]
  [ApiController]
  public class ContactController : ControllerBase
  {
    private readonly IContactExternalService _contactService;
    private readonly IContactsHelperService _contactHelperService;
    public ContactController(IContactExternalService contactService, IContactsHelperService contactHelperService)
    {
      _contactService = contactService;
      _contactHelperService = contactHelperService;
    }

    /// <summary>
    /// Get contact types
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET contacts/contact-types
    ///     
    ///  
    /// </remarks>
    [HttpGet("contact-types")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [SwaggerOperation(Tags = new[] { "Contact" })]
    [ProducesResponseType(typeof(ContactResponseDetail), 200)]
    public async Task<List<string>> GetContactTypes()
    {
      return await _contactHelperService.GetContactTypesAsync();
    }

    /// <summary>
    /// Allows a user to get contact reasons
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET contacts/contact-reasons   
    ///     
    ///
    /// </remarks>
    [HttpGet("contact-reasons")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    [SwaggerOperation(Tags = new[] { "Contact" })]
    [ProducesResponseType(typeof(List<ContactReasonInfo>), 200)]
    public async Task<List<ContactReasonInfo>> GetContactReasonInfoList()
    {
      return await _contactHelperService.GetContactPointReasonsAsync();
    }

    /// <summary>
    /// Create contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_CONTACT_TYPE, INVALID_CONTACT_VALUE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST contacts
    ///     {
    ///        "contactType": "EMAIL",
    ///        "contactValue": "contact@mail.com"
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Tags = new[] { "Contact" })]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<int> CreateContact(ContactRequestDetail contactRequestDetail)
    {
      return await _contactService.CreateAsync(contactRequestDetail);
    }

    /// <summary>
    /// Get contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET contacts/1
    ///     
    ///  
    /// </remarks>
    [HttpGet("{id}")]
    [SwaggerOperation(Tags = new[] { "Contact" })]
    [ProducesResponseType(typeof(ContactResponseDetail), 200)]
    public async Task<ContactResponseDetail> GetContact(int id)
    {
      return await _contactService.GetAsync(id);
    }

    /// <summary>
    /// Update contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <response  code="400">Bad request.
    /// Error Codes: INVALID_CONTACT_TYPE, INVALID_CONTACT_VALUE
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT contacts/1
    ///     {
    ///        "contactType": "EMAIL",
    ///        "contactValue": "contact@mail.com"
    ///     }
    ///     
    ///
    /// </remarks>
    [HttpPut("{id}")]
    [SwaggerOperation(Tags = new[] { "Contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task UpdateContact(int id, ContactRequestDetail contactRequestDetail)
    {
      await _contactService.UpdateAsync(id, contactRequestDetail);
    }

    /// <summary>
    /// Delete contact details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE contacts/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{id}")]
    [SwaggerOperation(Tags = new[] { "Contact" })]
    [ProducesResponseType(typeof(void), 200)]
    public async Task DeleteContact(int id)
    {
      await _contactService.DeleteAsync(id);
    }

  }
}
