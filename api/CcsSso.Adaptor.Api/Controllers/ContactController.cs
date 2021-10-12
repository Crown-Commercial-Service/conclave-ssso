using CcsSso.Adaptor.Domain.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.Controllers
{
  [Route("contacts")]
  [ApiController]
  public class ContactController : ControllerBase
  {
    private readonly IContactService _contactService;
    public ContactController(IContactService contactService)
    {
      _contactService = contactService;
    }

    #region Contact
    [HttpGet("{contactId}")]
    public async Task<Dictionary<string, object>> GetContact(int contactId)
    {
      return await _contactService.GetContactAsync(contactId);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContact(Dictionary<string, object> contactData)
    {
      var result = await _contactService.CreateContactAsync(contactData);

      return CreatedAtAction(nameof(GetContact), new { contactId = result }, result);
    }

    [HttpPut("{contactId}")]
    public async Task<int> UpdateContact(int contactId, Dictionary<string, object> contactData)
    {
      return await _contactService.UpdateContactAsync(contactId, contactData);
    } 
    #endregion

    #region User Contact
    [HttpGet("{contactId}/users")]
    public async Task<Dictionary<string, object>> GetUserContact(int contactId, [FromQuery(Name = "user-name")] string userName)
    {
      return await _contactService.GetUserContactAsync(contactId, userName);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateContact(Dictionary<string, object> contactData, [FromQuery(Name = "user-name")] string userName)
    {
      var result = await _contactService.CreateUserContactAsync(userName, contactData);

      return CreatedAtAction(nameof(GetUserContact), new { contactId = result }, result);
    }

    [HttpPut("{contactId}/users")]
    public async Task<int> UpdateContact(int contactId, Dictionary<string, object> contactData, [FromQuery(Name = "user-name")] string userName)
    {
      return await _contactService.UpdateUserContactAsync(contactId, userName, contactData);
    } 
    #endregion

    #region Organisation Contact
    [HttpGet("{contactId}/organisations/{organisationId}")]
    public async Task<Dictionary<string, object>> GetOrganisationContact(int contactId, string organisationId)
    {
      return await _contactService.GetOrganisationContactAsync(contactId, organisationId);
    }

    [HttpPost("organisations/{organisationId}")]
    public async Task<IActionResult> CreateOrganisationContact(string organisationId, Dictionary<string, object> contactData)
    {
      var result = await _contactService.CreateOrganisationContactAsync(organisationId, contactData);

      return CreatedAtAction(nameof(GetOrganisationContact), new { contactId = result, organisationId = organisationId }, result);
    }

    [HttpPut("{contactId}/organisations/{organisationId}")]
    public async Task<int> UpdateOrganisationContact(int contactId, string organisationId, Dictionary<string, object> contactData)
    {
      return await _contactService.UpdateOrganisationContactAsync(contactId, organisationId, contactData);
    } 
    #endregion

    #region Site Contact
    [HttpGet("{contactId}/organisations/{organisationId}/sites/{siteId}")]
    public async Task<Dictionary<string, object>> GetSiteContact(int contactId, string organisationId, int siteId)
    {
      return await _contactService.GetSiteContactAsync(contactId, organisationId, siteId);
    }

    [HttpPost("organisations/{organisationId}/sites/{siteId}")]
    public async Task<IActionResult> CreateSiteContact(string organisationId, int siteId, Dictionary<string, object> contactData)
    {
      var result = await _contactService.CreateSiteContactAsync(organisationId, siteId, contactData);

      return CreatedAtAction(nameof(GetSiteContact), new { contactId = result, organisationId = organisationId, siteId = siteId }, result);
    }

    [HttpPut("{contactId}/organisations/{organisationId}/sites/{siteId}")]
    public async Task<int> UpdateSiteContact(int contactId, string organisationId, int siteId, Dictionary<string, object> contactData)
    {
      return await _contactService.UpdateSiteContactAsync(contactId, organisationId, siteId, contactData);
    } 
    #endregion
  }
}
