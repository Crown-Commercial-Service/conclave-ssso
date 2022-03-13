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

    [HttpGet("country-details")]
    [SwaggerOperation(Tags = new[] { "Configuration" })]
    [ProducesResponseType(typeof(List<CountryDetail>), 200)]
    public async Task<List<CountryDetail>> GetCountryCodes()
    {
      return await _configurationDetailService.GetCountryDetailAsync();
    }
  }
}
