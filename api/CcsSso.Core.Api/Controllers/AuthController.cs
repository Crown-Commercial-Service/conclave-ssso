using CcsSso.Core.Authorisation;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Controllers
{
  [Route("auth")]
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

    [HttpPost("backchannel-logout")]
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

    [HttpPost("sessions")]
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
        Console.WriteLine($"CORE-API:- Refresh Token not available in /sessions, , Status: 400, Response: REFRESH_TOKEN_REQUIRED"); // Add no cookie log
        throw new CcsSsoException("REFRESH_TOKEN_REQUIRED");
      }
    }

    [HttpPost("sign-out")]
    public void Signout()
    {
      if (Request.Cookies.ContainsKey("conclave"))
      {
        Response.Cookies.Delete("conclave");
      }
    }

    [HttpGet("refresh-tokens")]
    public string GetRefreshToken()
    {
      string cookieName = "conclave";
      if (Request.Cookies.ContainsKey(cookieName))
      {
        Request.Cookies.TryGetValue(cookieName, out string refreshToken);
        return refreshToken;
      }
      Console.WriteLine($"CORE-API:- Cookie 'conclave' not available in /refesh-tokens , Status: 404"); // Add no cookie log
      throw new ResourceNotFoundException();
    }

    [HttpPost("passwords")]
    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_DEFAULT_USER")]
    public async Task ChangePassword(ChangePasswordDto changePassword)
    {
      await _authService.ChangePasswordAsync(changePassword);
    }

    [HttpPost("mfa-reset-notifications")]
    public async Task SendResetMfaNotification(MfaResetInfo mfaResetInfo)
    {
      await _authService.SendResetMfaNotificationAsync(mfaResetInfo);
    }

    [HttpPost("mfa-reset-by-tickets")]
    public async Task ResetMfaByTicket(MfaResetInfo mfaResetInfo)
    {
      await _authService.ResetMfaByTicketAsync(mfaResetInfo);
    }

    [ClaimAuthorise("ORG_ADMINISTRATOR", "ORG_USER_SUPPORT")]
    [HttpPost("mfa-reset-notification-by-admins")]
    public async Task SendResetMfaNotificationByAdmin(MfaResetInfo mfaResetInfo)
    {
      await _authService.SendResetMfaNotificationAsync(mfaResetInfo, true);
    }
  }
}

