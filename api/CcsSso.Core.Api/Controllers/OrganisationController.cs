using CcsSso.Core.Authorisation;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Contracts;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Api.Controllers
{
  [Route("organisations")]
  [ApiController]
  public class OrganisationController : ControllerBase
  {
    private readonly IOrganisationService _organisationService;

    public OrganisationController(IOrganisationService organisationService)
    {
      _organisationService = organisationService;
    }

    [HttpPost("registrations")]
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
    [HttpGet("{organisationId}")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<OrganisationDto> Get(string organisationId)
    {
      return await _organisationService.GetAsync(organisationId);
    }

    [HttpGet]
    [ClaimAuthorise("MANAGE_SUBSCRIPTIONS")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<OrganisationListResponse> GetAll([FromQuery(Name = "organisation-name")] string orgName, [FromQuery] ResultSetCriteria resultSetCriteria)
    {

      resultSetCriteria ??= new ResultSetCriteria
      {
        CurrentPage = 1,
        PageSize = 10
      };
      resultSetCriteria.CurrentPage = resultSetCriteria.CurrentPage <= 0 ? 1 : resultSetCriteria.CurrentPage;
      resultSetCriteria.PageSize = resultSetCriteria.PageSize <= 0 ? 10 : resultSetCriteria.PageSize;

      return await _organisationService.GetAllAsync(orgName, resultSetCriteria);
    }

    [HttpGet("users")]
    [ClaimAuthorise("ORG_USER_SUPPORT")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task<OrganisationUserListResponse> GetUsers(string name, [FromQuery] ResultSetCriteria resultSetCriteria)
    {

      resultSetCriteria ??= new ResultSetCriteria
      {
        CurrentPage = 1,
        PageSize = 10
      };
      resultSetCriteria.CurrentPage = resultSetCriteria.CurrentPage <= 0 ? 1 : resultSetCriteria.CurrentPage;
      resultSetCriteria.PageSize = resultSetCriteria.PageSize <= 0 ? 10 : resultSetCriteria.PageSize;

      return await _organisationService.GetUsersAsync(name, resultSetCriteria);
    }
  }
}
