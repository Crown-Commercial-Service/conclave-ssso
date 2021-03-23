using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
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
    public CiiController(ICiiService ciiService)
    {
      _ciiService = ciiService;
    }

    [HttpGet("{scheme}")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<CiiDto> Get(string scheme, [System.Web.Http.FromUri] string companyNumber)
    {
      return await _ciiService.GetAsync(scheme, companyNumber);
    }

    [HttpGet("GetSchemes")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<CiiSchemeDto[]> GetSchemes()
    {
      return await _ciiService.GetSchemesAsync();
    }

    [HttpGet("GetOrg")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<CiiDto> GetOrg(string id)
    {
      return await _ciiService.GetOrgAsync(id);
    }

    [HttpGet("GetOrgs")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<CiiDto[]> GetOrgs(string id)
    {
      return await _ciiService.GetOrgsAsync(id);
    }

    [HttpGet("GetIdentifiers")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<CiiDto> GetIdentifiers(string orgId, [System.Web.Http.FromUri] string scheme, [System.Web.Http.FromUri] string id)
    {
      return await _ciiService.GetIdentifiersAsync(orgId, scheme, id);
    }

    [HttpPost]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<CiiOrg> Post(CiiDto model)
    {
      var test = _ciiService.PostAsync(model).Result;
      return Newtonsoft.Json.JsonConvert.DeserializeObject<CiiOrg[]>(test)[0];
    }

    [HttpPut]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task<string> Put(CiiPutDto model)
    {
      return await _ciiService.PutAsync(model);
    }

    [HttpDelete]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task Delete(CiiDto model)
    {
      await _ciiService.DeleteAsync(model.identifier.id);
      // await _ciiService.DeleteAsyncWithBody(model);
    }

    [HttpDelete("DeleteOrg")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task DeleteOrg(string id)
    {
      await _ciiService.DeleteOrgAsync(id);
    }

    [HttpDelete("DeleteScheme")]
    [SwaggerOperation(Tags = new[] { "cii" })]
    public async Task DeleteScheme(string orgId, [System.Web.Http.FromUri] string scheme, [System.Web.Http.FromUri] string id)
    {
      await _ciiService.DeleteSchemeAsync(orgId, scheme, id);
    }
  }
}
