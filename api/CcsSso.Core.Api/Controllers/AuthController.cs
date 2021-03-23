using CcsSso.Core.Domain.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
      _authService = authService;
    }

    [HttpPost("backchannel_logout")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task BackchanelLogout([FromForm] IFormCollection logoutToken)
    {
      if (logoutToken.ContainsKey("logout_token"))
      {
        logoutToken.TryGetValue("logout_token", out StringValues token);
        bool isValid = await _authService.ValidateBackChannelLogoutTokenAsync(token.ToString());
        if (isValid)
        {
          // Clear the user session
        }
      }
    }
  }
}
