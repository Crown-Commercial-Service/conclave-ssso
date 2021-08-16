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

    [HttpGet("{scheme}")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiDto> Get(string scheme, [System.Web.Http.FromUri] string companyNumber)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      return await _ciiService.GetAsync(scheme, companyNumber, accessToken);
    }

    [HttpGet("GetSchemes")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiSchemeDto[]> GetSchemes()
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      return await _ciiService.GetSchemesAsync(accessToken);
    }

    [HttpGet("GetOrgs")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiDto[]> GetOrgs(string id)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      return await _ciiService.GetOrgsAsync(id, accessToken);
    }

    [HttpGet("GetIdentifiers")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task<CiiDto> GetIdentifiers(string orgId, [System.Web.Http.FromUri] string scheme, [System.Web.Http.FromUri] string id)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      return await _ciiService.GetIdentifiersAsync(orgId, scheme, id, accessToken);
    }

    [HttpPut("add-scheme")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task Put(CiiPutDto model)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      await _ciiService.PutAsync(model, accessToken);
    }

    [HttpDelete("delete-scheme")]
    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [SwaggerOperation(Tags = new[] { "Cii" })]
    public async Task DeleteScheme(string orgId, [System.Web.Http.FromUri] string scheme, [System.Web.Http.FromUri] string id)
    {
      var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
      await _ciiService.DeleteSchemeAsync(orgId, scheme, id, accessToken);
    }
  }
}
