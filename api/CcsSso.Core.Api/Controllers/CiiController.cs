using CcsSso.Core.Authorisation;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Api.Controllers
{
  [Route("cii")]
  [ApiController]
  public class CiiController : ControllerBase
  {
    private readonly ICiiService _ciiService;
    private IHttpContextAccessor _httpContextAccessor;

    public CiiController(ICiiService ciiService, IHttpContextAccessor httpContextAccessor)
    {
      _ciiService = ciiService;
      _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("schemes")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiSchemeDto[]> GetSchemes()
    {
      return await _ciiService.GetSchemesAsync();
    }

    [HttpGet("identifiers")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiDto> GetIdentifierDetails(string scheme, string identifier)
    {
      return await _ciiService.GetIdentifierDetailsAsync(scheme, identifier);
    }

    [HttpGet("organisation-details")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiDto> GetOrgDetails(string ciiOrganisationId)
    {
      return await _ciiService.GetOrgDetailsAsync(ciiOrganisationId);
    }

    [HttpGet("organisation-identifiers")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiDto> GetOrganisationIdentifierDetails(string ciiOrganisationId, string scheme, string identifier)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      return await _ciiService.GetOrganisationIdentifierDetailsAsync(ciiOrganisationId, scheme, identifier, accessToken);
    }

    [HttpPut("add-scheme")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task AddScheme(string ciiOrganisationId, string scheme, string identifier)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      await _ciiService.AddSchemeAsync(ciiOrganisationId, scheme, identifier, accessToken);
    }

    [HttpDelete("delete-scheme")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task DeleteScheme(string ciiOrganisationId, string scheme, string identifier)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      await _ciiService.DeleteSchemeAsync(ciiOrganisationId, scheme, identifier, accessToken);
    }
  }
}
