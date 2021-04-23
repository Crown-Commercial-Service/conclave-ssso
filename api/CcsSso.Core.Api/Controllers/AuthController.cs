using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Exceptions;
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

    [HttpPost("save_refresh_token")]
    public void SaveRefreshToken(TokenDetails tokenDetails)
    {
      if (!string.IsNullOrEmpty(tokenDetails.RefreshToken))
      {
        CookieOptions cookieOptions = new CookieOptions()
        {
          SameSite = SameSiteMode.None,
          Secure = true,
          HttpOnly = true
        };
        string cookieName = "refreshToken";
        if (!Request.Cookies.ContainsKey(cookieName))
        {
          Response.Cookies.Append(cookieName, tokenDetails.RefreshToken, cookieOptions);
        }
        else
        {
          Response.Cookies.Delete(cookieName);
          Response.Cookies.Append(cookieName, tokenDetails.RefreshToken, cookieOptions);
        }
      }
      else
      {
        throw new CcsSsoException("REFRESH_TOKEN_REQUIRED");
      }
    }

    [HttpPost("sign_out")]
    public void Signout()
    {
      if (Request.Cookies.ContainsKey("refreshToken"))
      {
        Response.Cookies.Delete("refreshToken");
      }
    }

    [HttpGet("get_refresh_token")]
    public string GetRefreshToken()
    {
      string cookieName = "refreshToken";
      if (Request.Cookies.ContainsKey(cookieName))
      {
        Request.Cookies.TryGetValue(cookieName, out string refreshToken);
        return refreshToken;
      }
      throw new ResourceNotFoundException();
    }

    [HttpPost("change_password")]
    public async Task ChangePassword(ChangePasswordDto changePassword)
    {
      await _authService.ChangePasswordAsync(changePassword);
    }
  }
}

