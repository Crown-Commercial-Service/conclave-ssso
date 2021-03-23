using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Controllers
{
  //[Route("[controller]")]
  [ApiController]
  public class SecurityController : ControllerBase
  {

    private readonly ISecurityService _securityService;
    private readonly IUserManagerService _userManagerService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IJwtTokenHandler _jwtHandler;
    public SecurityController(ISecurityService securityService, IUserManagerService userManagerService,
      ApplicationConfigurationInfo applicationConfigurationInfo, IJwtTokenHandler jwtHandler)
    {
      _securityService = securityService;
      _userManagerService = userManagerService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _jwtHandler = jwtHandler;
    }

    /// <summary>
    /// Authenticates a user and issues 3 tokens
    /// </summary>
    /// <response code="200">For successfull authentication id token, refresh token and access token are issued.
    /// When the temporary password is used ChallengeName = NEW_PASSWORD_REQUIRED and ChallengeRequired = true with a valid SessionId
    /// 
    /// </response>
    /// <response  code="401">Authentication fails</response>
    /// <response  code="400">Password reset pending. code: PASSWORD_RESET_REQUIRED</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /login
    ///     {
    ///        "username": "helen@xxx.com",
    ///        "userpassword": "1234",
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Produces("application/json")]
    [Route("security/login")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<AuthResultDto> Login([FromBody] AuthRequest authRequest)
    {
      var authResponse = await _securityService.LoginAsync(authRequest.UserName, authRequest.UserPassword);
      return authResponse;
    }

    /// <summary>
    /// Redirects the user to the configured identity and access management login URL
    /// </summary>
    [HttpGet("security/authorize")]
    [ProducesResponseType(302)]
    [SwaggerOperation(Tags = new[] { "security" })]
    public IActionResult Authorize(string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt = null)
    {
      var url = _securityService.GetAuthenticationEndPoint(scope, response_type, client_id, redirect_uri, code_challenge_method, code_challenge, prompt);
      return Redirect(url);
    }

    /// <summary>
    /// Issues new security tokens when provide any type of following (auth code, refresh token) 
    /// </summary>
    /// <response code="200">When grant type is "authorization_code" returns id token,refresh token and access token.
    /// When grant type is "refresh_token" returns id token and access token</response>
    /// <response  code="400">
    /// Code: INVALID_TOKEN (Refresh token and auth code are empty),
    /// Code: UNSUPPORTED_GRANT_TYPE (Invalid grant type)
    /// </response>
    /// <remarks>
    /// Sample requests:
    ///
    ///     POST /token
    ///     {
    ///        "code": "abcs123",
    ///        "refreshtoken": null,
    ///        "granttype:"authorization_code",
    ///        "redirect_uri":"http://redirect_url/"
    ///     }
    ///
    ///     POST /token
    ///     {
    ///        "code": null,
    ///        "refreshtoken": "abcs123",
    ///        "granttype:"refresh_token",
    ///        "redirect_uri":"http://redirect_url/"
    ///     }
    ///
    /// </remarks>
    [HttpPost("security/token")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<TokenResponseInfo> Token(TokenRequestInfo tokenRequestInfo)
    {
      // Sessions are handled in two places for a user and they are as Auth0 & Security api (aka CCS-SSO session cookie).
      // Auth0 session is given the highest priority as it used to generate tokens. Hence, CCS-SSO session will be
      // extented for valid Auth0 sessions.

      string opbsCookieName = "opbs";
      DateTime expiresOnUTC = DateTime.UtcNow.AddMinutes(_applicationConfigurationInfo.SessionConfig.SessionTimeoutInMinutes);

      CookieOptions opbsCookieOptions = new CookieOptions()
      {
        Expires = expiresOnUTC,
        // Since it's not clear the way the project is build (debug or release), this was always set to None with secure True
        SameSite = SameSiteMode.None,
        Secure = true
        //#if DEBUG
        //        SameSite = SameSiteMode.None,
        //        Secure = true
        //#else
        //          SameSite = SameSiteMode.Lax    // Need to verify
        //#endif
      };
      string opbsValue;
      // Generate OPBS cookie if not exists in request
      if (!Request.Cookies.ContainsKey(opbsCookieName))
      {
        opbsValue = Guid.NewGuid().ToString();
        Response.Cookies.Append(opbsCookieName, opbsValue, opbsCookieOptions);
      }
      else
      {
        Request.Cookies.TryGetValue(opbsCookieName, out opbsValue);
        Response.Cookies.Delete(opbsCookieName);
        Response.Cookies.Append(opbsCookieName, opbsValue, opbsCookieOptions);
      }
      var redirectUri = new Uri(tokenRequestInfo.RedirectUrl);
      var host = redirectUri.AbsoluteUri.Split(redirectUri.AbsolutePath)[0];

      CookieOptions httpCookieOptions = new CookieOptions()
      {
        HttpOnly = true,
        Expires = expiresOnUTC,
#if DEBUG
        SameSite = SameSiteMode.None,
        Secure = true
#else
          SameSite = SameSiteMode.Lax    // Need to verify
#endif
      };
      string sessionCookie = "ccs-sso";
      string sid = string.Empty;
      if (!Request.Cookies.ContainsKey(sessionCookie))
      {
        sid = Guid.NewGuid().ToString();
        Response.Cookies.Append(sessionCookie, sid, httpCookieOptions);
      }
      else
      {
        Request.Cookies.TryGetValue(sessionCookie, out sid);
        Response.Cookies.Delete(sessionCookie);
        //Re-assign the same session id with new expiration time
        Response.Cookies.Append(sessionCookie, sid, httpCookieOptions);
      }

      var idToken = await _securityService.GetRenewedTokenAsync(tokenRequestInfo, opbsValue, host, sid);

      CookieOptions visitedSiteCookieOptions = new CookieOptions()
      {
        HttpOnly = true,
        Expires = expiresOnUTC,
#if DEBUG
        SameSite = SameSiteMode.None,
        Secure = true
#else
          SameSite = SameSiteMode.Lax    // Need to verify
#endif
      };
      string visitedSiteCookie = "ccs-sso-visitedsites";
      if (!Request.Cookies.ContainsKey(visitedSiteCookie))
      {
        Response.Cookies.Append(visitedSiteCookie, tokenRequestInfo.ClientId, httpCookieOptions);
      }
      else
      {
        Request.Cookies.TryGetValue(visitedSiteCookie, out string visitedSites);
        var visitedSiteList = visitedSites.Split(',').ToList();
        if (!visitedSiteList.Contains(tokenRequestInfo.ClientId))
        {
          visitedSiteList.Add(tokenRequestInfo.ClientId);
          visitedSites = string.Join(",", visitedSiteList);
          Response.Cookies.Delete(visitedSiteCookie);
          Response.Cookies.Append(visitedSiteCookie, visitedSites, visitedSiteCookieOptions);
        }
      }
      return idToken;
    }

    [HttpGet("security/checksession")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ContentResult> CheckSession()
    {
      var path = "./Static/OPIFrame.html";
      var fileContents = await System.IO.File.ReadAllTextAsync(path);
      return new ContentResult
      {
        Content = fileContents,
        ContentType = "text/html"
      };
    }

    /// <summary>
    /// Registers a new user in Identity Provider 
    /// </summary>
    /// <response code="201">user is created successfully</response>
    /// <response  code="400">
    /// Code: ERROR_FIRSTNAME_REQUIRED (first name is required),
    /// Code: ERROR_LASTNAME_REQUIRED (last name is required)
    /// Code: ERROR_EMAIL_REQUIRED (email is required)
    /// Code: ERROR_EMAIL_FORMAT (invaid email)
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /register
    ///     {
    ///        "UserName": "helen@xxx.com",
    ///        "Email": "helen@xxx.com",
    ///        "FirstName":"Helen",
    ///        "LastName":"Fox"
    ///     }
    /// </remarks>
    [HttpPost("security/register")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<UserRegisterResult> Register(UserInfo userInfo)
    {
      var userRegisterResult = await _userManagerService.CreateUserAsync(userInfo);
      return userRegisterResult;
    }

    /// <summary>
    /// Returns all external identity providers that are listed
    /// </summary>
    /// <response code="200">List of external identity providers</response>
    [HttpGet("security/externalidp")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    public async Task<List<IdentityProviderInfoDto>> GetIdentityProvidersList()
    {
      var idProviders = await _securityService.GetIdentityProvidersListAsync();
      return idProviders;
    }

    /// <summary>
    /// Updates a user in the identity provider
    /// </summary>
    /// <response code="204">Successfully updates a user</response>
    /// <response  code="400">
    /// Code: ERROR_FIRSTNAME_REQUIRED (FirstName Required),
    /// Code: ERROR_LASTNAME_REQUIRED (LastName Required)
    /// Code: ERROR_EMAIL_REQUIRED (Email Required)
    /// Code: ERROR_EMAIL_FORMAT (Invaid Email)
    /// </response>
    /// <remarks>
    /// Sample requests:
    ///
    ///     POST /updateuser
    ///     {
    ///        "id":"123",
    ///        "userName": "helen@xxx.com",
    ///        "Email": "helen@xxx.com",
    ///        "FirstName":"Helen",
    ///        "LastName":"Fox",
    ///        "Role":"Admin",
    ///        "Groups":["CCS SITE 1,CCS SITE 2"],
    ///        "ProfilePageUrl:"<URL>Sample Profile URL</URL>"
    ///     }
    /// </remarks>
    [HttpPost("security/updateuser")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task UpdateUser(UserInfo userInfo)
    {
      await _userManagerService.UpdateUserAsync(userInfo);
    }

    /// <summary>
    /// Change the old password to the new password
    /// </summary>
    /// <response code="204">password change is successful</response>
    /// <response  code="400">
    /// Code: ACCESS_TOKEN_REQUIRED (uccess token is required),
    /// Code: NEW_PASSWORD_REQUIRED (new password is required)
    /// Code: OLD_PASSWORD_REQUIRED (old password is required)
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /changepassword
    ///     {
    ///        "UserId":"123",
    ///        "AccessToken": "1234",
    ///        "NewPassword": "newpassword",
    ///        "OldPassword":"oldpassword"
    ///     }
    /// </remarks>
    [HttpPost("security/changepassword")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task ChangePassword(ChangePasswordDto changePasswordDto)
    {
      await _securityService.ChangePasswordAsync(changePasswordDto);
    }

    /// <summary>
    /// Change the temporary password to a new password after the initial login (after registration) 
    /// </summary>
    /// <response code="200">id token, access token and refresh token</response>
    /// <response code="401">When invalid session id/user name is provided</response>
    /// <response  code="400">
    /// Code: USERNAME_REQUIRED (username is required),
    /// Code: NEW_PASSWORD_REQUIRED (new password is required)
    /// Code: SESSION_ID_REQUIRED (session id is required)
    /// Code: ERROR_PASSWORD_POLICY_MISMATCH (password policy mismatched)
    /// </response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /passwordchallenge
    ///     {
    ///        "UserName": "helen@xxx.com",
    ///        "SessionId": "session1",
    ///        "NewPassword":"newpassword"
    ///     }
    /// </remarks>
    [HttpPost("security/passwordchallenge")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [SwaggerOperation(Tags = new[] { "security" })]
    public async Task<AuthResultDto> RespondToNewPasswordRequired(PasswordChallengeDto passwordChallengeDto)
    {
      var responce = await _securityService.ChangePasswordWhenPasswordChallengeAsync(passwordChallengeDto);
      return responce;
    }

    /// <summary>
    /// Initialize a password reset request. A notification with a code will be sent to the user
    /// </summary>
    /// <response code="204">Successfully initialize reset password</response>
    /// <response  code="400">
    /// Code: USERNAME_REQUIRED (UserName is required)
    /// </response>
    /// <remarks>
    /// Sample request:
    /// POST /passwordresetrequest
    /// {
    ///   "helen@xxx.com"
    /// }
    /// </remarks>

    [HttpPost("security/passwordresetrequest")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task InitiateResetPassword([FromBody] string userName)
    {
      await _securityService.InitiateResetPasswordAsync(userName);
    }

    /// <summary>
    /// Reset the password
    /// </summary>
    /// <response code="204">Successfully reset the password</response>
    /// <response  code="400">
    /// Code: VERIFICATION_CODE_REQUIRED (Verification code is required),
    /// Code: USERNAME_REQUIRED (UserName is required),
    /// Code: NEW_PASSWORD_REQUIRED (New password is required)
    /// </response>
    /// <remarks>
    /// Sample requests:
    ///
    ///     POST /passwordreset
    ///     {
    ///        "UserName": "helen@xxx.com",
    ///        "VerificationCode": "1234",
    ///        "NewPassword":"XXXX"
    ///     }
    /// </remarks>
    [HttpPost("security/passwordreset")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task ResetPassword(ResetPasswordDto resetPassword)
    {
      await _securityService.ResetPasswordAsync(resetPassword);
    }

    /// <summary>
    /// Clear out the user session details
    /// </summary>
    /// <response code="302">Successfully sign out the user</response>
    /// <response  code="400">
    /// Code: REDIRECT_URI_REQUIRED (Redirect URI is required)
    /// </response>
    /// <remarks>
    /// Sample request:
    /// GET /logout/http://redirecturi.com
    /// </remarks>
    [HttpGet("security/logout")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(302)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LogOut(string clientId, string redirecturi)
    {
      var url = await _securityService.LogoutAsync(clientId, redirecturi);
      if (Request.Cookies.ContainsKey("opbs"))
      {
        Response.Cookies.Delete("opbs");
      }
      // delete the session cookie
      string sessionCookie = "ccs-sso";
      if (Request.Cookies.ContainsKey(sessionCookie))
      {
        Request.Cookies.TryGetValue(sessionCookie, out string sid);
        Response.Cookies.Delete(sessionCookie);


        string visitedSiteCookie = "ccs-sso-visitedsites";
        if (Request.Cookies.ContainsKey(visitedSiteCookie))
        {
          Request.Cookies.TryGetValue(visitedSiteCookie, out string visitedSites);
          var visitedSiteList = visitedSites.Split(',').ToList();
          // Perform back chanel logout - This should be performed as a queue triggered background job
          await _securityService.PerformBackChannelLogoutAsync(sid, visitedSiteList);
          Response.Cookies.Delete(visitedSiteCookie);
        }
      }

      return Redirect(url);
    }

    /// <summary>
    /// Redirect to the identity provider login
    /// </summary>
    /// <response code="302">Successfully redirect the user</response>
    [HttpGet("security/redirect_to_identity_provider")]
    [ProducesResponseType(302)]
    [SwaggerOperation(Tags = new[] { "security" })]
    public async Task<IActionResult> RedirectToExternalIdentityProvider()
    {
      var url = await _securityService.GetIdentityProviderAuthenticationEndPointAsync();
      return Redirect(url);
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    /// <response code="302">Successfully redirect the user</response>
    [HttpGet("security/revoke")]
    [ProducesResponseType(302)]
    [SwaggerOperation(Tags = new[] { "security" })]
    public async Task RevokeToken(string refreshToken)
    {
      await _securityService.RevokeTokenAsync(refreshToken);
    }

    /// <summary>
    /// This endpoint was introduced by Lee and lead to some build errors.
    /// Therefore change the method content.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    [HttpPost("security/redirect_endpoint")]
    [SwaggerOperation(Tags = new[] { "security" })]
    public Task<string> RedirectEndPoint(string url)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// This endpoint was introduced by Lee and lead to some build errors.
    /// Therefore change the method content. 
    /// </summary>
    /// <param name="userInfo"></param>
    /// <returns></returns>
    [HttpPost("register/external")]
    [SwaggerOperation(Tags = new[] { "security" })]
    public Task RegisterExternal(UserInfo userInfo)
    {
      throw new NotImplementedException();
    }

    [HttpGet("user_email")]
    [SwaggerOperation(Tags = new[] { "security" })]
    public async Task<UserClaims> GetEmail(string accessToken)
    {
      return await _userManagerService.GetUserAsync(accessToken);
    }




    /// <summary>
    /// Validates the token
    /// </summary>
    /// <response code="200">Successfully validated the token</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="400">
    /// Code: TOKEN_REQUIRED (Token is required)
    /// Code: CLIENTID_REQUIRED (ClientId is required)
    /// </response>
    /// <remarks>
    /// Sample request:
    /// POST security/validate_token?clientid=xxx Authorization: Bearer vF9dft4qmT
    /// </remarks>
    [HttpPost("security/validate_token")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public bool ValidateToken(string clientId)
    {
      if(Request.Headers.ContainsKey("Authorization"))
      {
        var bearerToken = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(bearerToken))
        {
          var token = bearerToken.Split(' ').Last();
          return _securityService.ValidateToken(clientId, token);
        }       
      }
      throw new UnauthorizedAccessException();
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    /// <response code="204">Successfully delete the user</response>
    /// <response code="404">User not found </response>
    /// <response  code="400">
    /// Code: INVALID_EMAIL (Invalid Email address)
    /// </response>
    [HttpPost("security/deleteuser")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task DeleteUser([FromBody] string email)
    {
      await _userManagerService.DeleteUserAsync(email);
    }
  }
}
