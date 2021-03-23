using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Api.Controllers
{
  [Route("organisation")]
  [ApiController]
  public class OrganisationController : ControllerBase
  {

    private readonly IOrganisationService _organisationService;
    public OrganisationController(IOrganisationService organisationService)
    {
      _organisationService = organisationService;
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
  }
}
