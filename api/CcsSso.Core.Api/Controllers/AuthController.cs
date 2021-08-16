using CcsSso.Core.Authorisation;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Dtos;
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
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    public AuthController(IAuthService authService, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _authService = authService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
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

    [HttpPost("create_session")]
    public void CreateSession(TokenDetails tokenDetails)
    {
      if (!string.IsNullOrEmpty(tokenDetails.RefreshToken))
      {
        CookieOptions cookieOptions = new CookieOptions()
        {
          Secure = true,
          HttpOnly = true
        };
        if(!string.IsNullOrEmpty(_applicationConfigurationInfo.CustomDomain))
        {
          cookieOptions.SameSite = SameSiteMode.Lax;
          cookieOptions.Domain = _applicationConfigurationInfo.CustomDomain;
        }
        else
        {
          cookieOptions.SameSite = SameSiteMode.None;
        }

        string cookieName = "conclave";
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
      if (Request.Cookies.ContainsKey("conclave"))
      {
        Response.Cookies.Delete("conclave");
      }
    }

    [HttpGet("get_refresh_token")]
    public string GetRefreshToken()
    {
      string cookieName = "conclave";
      if (Request.Cookies.ContainsKey(cookieName))
      {
        Request.Cookies.TryGetValue(cookieName, out string refreshToken);
        return refreshToken;
      }
      throw new ResourceNotFoundException();
    }

    [HttpPost("change_password")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    public async Task ChangePassword(ChangePasswordDto changePassword)
    {
      await _authService.ChangePasswordAsync(changePassword);
    }

    [HttpPost("send_reset_mfa_Notification")]
    public async Task SendResetMfaNotification(MfaResetInfo mfaResetInfo)
    {
      await _authService.SendResetMfaNotificationAsync(mfaResetInfo);
    }

    [HttpPost("reset_mfa_by_ticket")]
    public async Task ResetMfaByTicket(MfaResetInfo mfaResetInfo)
    {
      await _authService.ResetMfaByTicketAsync(mfaResetInfo);
    }

    [ClaimAuthorise("ORG_ADMINISTRATOR")]
    [HttpPost("send_reset_mfa_notification_by_admin")]
    public async Task SendResetMfaNotificationByAdmin(MfaResetInfo mfaResetInfo)
    {
      await _authService.SendResetMfaNotificationAsync(mfaResetInfo, true);
    }
  }
}

