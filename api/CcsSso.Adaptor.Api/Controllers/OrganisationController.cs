using CcsSso.Adaptor.Domain.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.Controllers
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

    [HttpGet("{organisationId}")]
    public async Task<Dictionary<string, object>> GetOrganisations(string organisationId)
    {
      return await _organisationService.GetOrganisationAsync(organisationId);
    }
  }
}
