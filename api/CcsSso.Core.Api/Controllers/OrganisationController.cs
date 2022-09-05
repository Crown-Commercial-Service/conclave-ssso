using CcsSso.Core.Authorisation;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
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
    private readonly IBulkUploadService _bulkUploadService;

    public OrganisationController(IOrganisationService organisationService, IBulkUploadService bulkUploadService)
    {
      _organisationService = organisationService;
      _bulkUploadService = bulkUploadService;
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

    [HttpGet("orgs-by-name")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    [ProducesResponseType(typeof(OrganisationDto), 200)]
    [ProducesResponseType(204)]
    public async Task<List<OrganisationDto>> GetByName([FromQuery(Name = "organisation-name")] string orgName, [FromQuery(Name = "exact-match")] bool isExact = true)
    {
      return await _organisationService.GetByNameAsync(orgName, isExact);
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

    [HttpPost("org-admin-join-notification")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task SendOrgAdminJoinRequestEmail(OrganisationJoinRequest organisationJoinRequest)
    {
      await _organisationService.NotifyOrgAdminToJoinAsync(organisationJoinRequest);
    }

    [HttpPost("{organisationId}/bulk-users")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task<AcceptedResult> InitiateBulkUserUpload(string organisationId, IFormFile file)
    {
      var result = await _bulkUploadService.BulkUploadUsersAsync(organisationId, file);
      return new AcceptedResult($"organisations/{organisationId}/bulk-users/status", result);
    }

    [HttpGet("{organisationId}/bulk-users/status")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "Organisation" })]
    public async Task<BulkUploadStatusResponse> GetBulkUserUploadStatus(string organisationId, string id)
    {
      var result = await _bulkUploadService.CheckBulkUploadStatusAsync(organisationId, id);
      return result;
    }

    [HttpGet("{organisationId}/users")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "Organisation User" })]
    [ProducesResponseType(typeof(UserListResponse), 200)]
    public int GetUserAffectedByRemovedIdps(string organisationId, [FromQuery(Name = "idps")] string idps)
    {
      var result = _organisationService.GetAffectedUsersByRemovedIdp(organisationId, idps);
      return result;
    }
  }
}
