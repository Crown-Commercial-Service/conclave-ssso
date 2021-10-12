using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Api.Controllers
{
  [Route("users")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
      _userService = userService;
    }

    [HttpGet("permissions")]
    [SwaggerOperation(Tags = new[] { "User" })]
    public async Task<List<ServicePermissionDto>> GetPermissions([FromQuery(Name = "user-name")] string userName, [FromQuery(Name = "service-client-id")] string serviceClientId)
    {
      return await _userService.GetPermissions(userName, serviceClientId);
    }

    [HttpPost("nominees")]
    [SwaggerOperation(Tags = new[] { "User" })]
    public async Task Nominate([FromBody] string email)
    {
      await _userService.NominateUserAsync(email);
    }

    [HttpPost("activation-emails")]
    [Consumes("application/x-www-form-urlencoded")]
    [SwaggerOperation(Tags = new[] { "User" })]
    public async Task SendUserActivationEmail(IFormCollection userDetails)
    {
      string registrationDetailsCookie = "rud";
      if (Request.Cookies.ContainsKey(registrationDetailsCookie))
      {
        Request.Cookies.TryGetValue(registrationDetailsCookie, out string details);
        if (details == "as")
        {
          userDetails.TryGetValue("email", out StringValues email);
          await _userService.SendUserActivationEmailAsync(email);
          CookieOptions httpCookieOptions = new CookieOptions()
          {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true
          };
          //"ras" stands for activation email re-sent
          Response.Cookies.Append(registrationDetailsCookie, "ras", httpCookieOptions);
        }
      }
    }
  }
}
