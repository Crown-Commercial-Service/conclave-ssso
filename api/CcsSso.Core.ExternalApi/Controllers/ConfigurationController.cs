using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Controllers
{
  [Route("configurations")]
  [ApiController]
  public class ConfigurationController : ControllerBase
  {
    private IConfigurationDetailService _configurationDetailService;
    public ConfigurationController(IConfigurationDetailService configurationDetailService)
    {
      _configurationDetailService = configurationDetailService;
    }

    /// <summary>
    /// Allows a user to get identity provider details
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET configurations/identity-providers
    ///     
    ///     
    ///
    /// </remarks>
    [HttpGet("identity-providers")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<IdentityProviderDetail>), 200)]
    public async Task<List<IdentityProviderDetail>> GetIdentityProviders()
    {
      return await _configurationDetailService.GetIdentityProvidersAsync();
    }

    [HttpGet("roles")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<OrganisationRole>), 200)]
    public async Task<List<OrganisationRole>> GetRoles()
    {
      return await _configurationDetailService.GetRolesAsync();
    }

    [HttpGet("services")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    public async Task<List<CcsServiceInfo>> GetCcsServices()
    {
      return await _configurationDetailService.GetCcsServicesAsync();
    }

    [HttpGet("services/{clientId}")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    public async Task<ServiceProfile> GetServiceProfile(string clientId, [FromQuery(Name = "organisation-id")] string organisationId)
    {
      return await _configurationDetailService.GetServiceProfieAsync(clientId, organisationId);
    }

    [HttpGet("country-details")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<CountryDetail>), 200)]
    public async Task<List<CountryDetail>> GetCountryCodes()
    {
      return await _configurationDetailService.GetCountryDetailAsync();
    }

    /// <summary>
    /// Return list of roles that require approval before assignment
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="400">Bad request.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET configurations/approval/roles
    /// </remarks>
    [HttpGet("approve/roles")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<OrganisationRole>), 200)]
    public async Task<List<OrganisationRole>> GetRolesRequireApproval()
    {
      return await _configurationDetailService.GetRolesRequireApprovalAsync();
    }

    /// <summary>
    /// Return all service role groups
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET configurations/servicerolegroups
    /// </remarks>
    [HttpGet("servicerolegroups")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<ServiceRoleGroup>), 200)]
    public async Task<List<ServiceRoleGroup>> GetServiceRoleGroups()
    {
      return await _configurationDetailService.GetServiceRoleGroupsAsync();
    }

    /// <summary>
    /// Return list of service role groups that require approval before assignment
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="400">Bad request.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET configurations/approval/servicerolegroups
    /// </remarks>
    [HttpGet("approve/servicerolegroups")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<ServiceRoleGroup>), 200)]
    public async Task<List<ServiceRoleGroup>> GetServiceRoleGroupsRequireApproval()
    {
      return await _configurationDetailService.GetServiceRoleGroupsRequireApprovalAsync();
    }
  }
}
