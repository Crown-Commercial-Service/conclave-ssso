using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CcsSso.Api.Controllers
{
  [Route("organisation")]
  [ApiController]
  public class OrganisationController : ControllerBase
  {
    private readonly ICiiService _ciiService;
    private readonly IOrganisationService _organisationService;
    private readonly IContactService _contactService;
    private readonly IUserService _userService;

    public OrganisationController(IOrganisationService organisationService, ICiiService ciiService, IContactService contactService, IUserService userService)
    {
      _organisationService = organisationService;
      _ciiService = ciiService;
      _contactService = contactService;
      _userService = userService;
    }

    /// <summary>
    /// Method to delete an organisation.
    /// </summary>
    /// <response  code="200">Successfully deleted</response>
    /// <response  code="401">Authentication fails</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /organisation/1
    ///     
    ///
    /// </remarks>
    [HttpDelete("{id}")]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    public async Task Delete(int id)
    {
      await _organisationService.DeleteAsync(id);
    }

    /// <summary>
    /// Method to get a organisation by its id.
    /// </summary>
    /// <response  code="200">organisation details</response>
    /// <response  code="204">No content</response>
    /// <response  code="401">Authentication failed</response>
    /// <remarks>
    /// Sample request: GET /organisation/1
    /// </remarks>
    [HttpGet("{id}")]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<OrganisationDto> Get(string id)
    {
      return await _organisationService.GetAsync(id);
    }

    [HttpGet("getAll")]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<List<OrganisationDto>> GetAll()
    {
      return await _organisationService.GetAllAsync();
    }

    /// <summary>
    /// Method to create an organisation
    /// </summary>
    /// <response  code="200">Organisation Id</response>
    /// <response  code="400">Bad Request</response>
    /// <remarks>
    /// Sample request:
    /// POST /organisation
    /// {
    ///    "ciiOrganisationId": "12345678910",
    ///    "organisationUri": "http://www.google.com",
    ///    "rightToBuy": true,
    ///    "partyId": 1,
    /// }
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    // [ProducesResponseType(typeof(int), 200)]
    // [ProducesResponseType(401)]
    // [ProducesResponseType(typeof(string), 400)]
    public async Task<int> Post(OrganisationDto model)
    {
      return await _organisationService.CreateAsync(model);
    }

    [HttpPut]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    public async Task Put(OrganisationDto model)
    {
      await _organisationService.PutAsync(model);
    }

    [HttpGet("getUsers")]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    public async Task<List<OrganisationUserDto>> GetUsers()
    {
      return await _organisationService.GetUsersAsync();
    }

    [HttpPost("rollback")]
    [SwaggerOperation(Tags = new[] { "organisation" })]
    // [ProducesResponseType(typeof(int), 200)]
    // [ProducesResponseType(401)]
    // [ProducesResponseType(typeof(string), 400)]
    public async Task Rollback(OrganisationRollbackDto model)
    {
      if (!String.IsNullOrEmpty(model.CiiOrganisationId))
      {
        try
        {
          await _ciiService.DeleteAsync(model.CiiOrganisationId);
        }
        catch (System.Exception ex)
        {
          System.Console.Write(ex);
        }
      }
      if (!String.IsNullOrEmpty(model.OrganisationId))
      {
        try
        {
          await _organisationService.DeleteAsync(Int32.Parse(model.OrganisationId));
        }
        catch (System.Exception ex)
        {
          System.Console.Write(ex);
        }
      }
      if (!String.IsNullOrEmpty(model.ContactId))
      {
        try
        {
          await _contactService.DeleteAsync(Int32.Parse(model.ContactId));
        }
        catch (System.Exception ex)
        {
          System.Console.Write(ex);
        }
      }
      if (!String.IsNullOrEmpty(model.UserId))
      {
        try
        {
          await _userService.DeleteAsync(Int32.Parse(model.UserId));
        }
        catch (System.Exception ex)
        {
          System.Console.Write(ex);
        }
      }
    }
  }
}
