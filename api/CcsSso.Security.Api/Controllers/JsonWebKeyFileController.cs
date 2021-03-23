using CcsSso.Security.Domain.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Controllers
{
  [ApiController]
  public class JsonWebKeyFileController : ControllerBase
  {
    private readonly ISecurityService _securityService;

    public JsonWebKeyFileController(ISecurityService securityService)
    {
      _securityService = securityService;
    }

    [Route(".well-known/jwks.json")]
    [HttpGet]
    public string GenerateJsonWebTokens()
    {
      return _securityService.GetJsonWebKeyTokens();
    }
  }
}
