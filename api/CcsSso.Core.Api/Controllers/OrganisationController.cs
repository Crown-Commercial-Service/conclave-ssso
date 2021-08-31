using CcsSso.Core.Authorisation;
using CcsSso.Domain.Contracts;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Http;
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

    [HttpPost("register")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task<string> Register(OrganisationRegistrationDto organisationRegistrationDto)
    {
      CookieOptions httpCookieOptions = new CookieOptions()
      {
        HttpOnly = true,
        SameSite = SameSiteMode.None,
        Secure = true
      };
      string registrationDetailsCookie = "rud";
      Response.Cookies.Delete(registrationDetailsCookie);
      //"as" stands for activation email sent
      Response.Cookies.Append(registrationDetailsCookie, "as", httpCookieOptions);
      return await _organisationService.RegisterAsync(organisationRegistrationDto);
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
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<OrganisationDto> Get(string id)
    {
      return await _organisationService.GetAsync(id);
    }

    [HttpGet("getAll")]
    [ClaimAuthorise("MANAGE_SUBSCRIPTIONS")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<List<OrganisationDto>> GetAll(string orgName)
    {
      return await _organisationService.GetAllAsync(orgName);
    }

    [HttpGet("getUsers")]
    [ClaimAuthorise("ORG_USER_SUPPORT")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task<List<OrganisationUserDto>> GetUsers(string name)
    {
      return await _organisationService.GetUsersAsync(name);
    }
  }
}
