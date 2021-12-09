using CcsSso.Security.Api.Models;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Controllers
{
  [ApiController]
  public class SecurityController : ControllerBase
  {

    private readonly ISecurityService _securityService;
    private readonly IUserManagerService _userManagerService;
    private readonly ISecurityCacheService _securityCacheService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly ICryptographyService _cryptographyService;
    public SecurityController(ISecurityService securityService, IUserManagerService userManagerService,
      ApplicationConfigurationInfo applicationConfigurationInfo,
      ISecurityCacheService securityCacheService, ICryptographyService cryptographyService)
    {
      _securityService = securityService;
      _userManagerService = userManagerService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _securityCacheService = securityCacheService;
      _cryptographyService = cryptographyService;
    }

    /// <summary>
    /// Authenticates a user and issues 3 tokens
    /// </summary>
    /// <response code="200">For successfull authentication id token, refresh token and access token are issued.
    /// </response>
    /// <response  code="401">Authentication fails</response>
    /// <response  code="400">Password reset pending. code: PASSWORD_RESET_REQUIRED</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /security/test/oauth/token
    ///     {
    ///        "username": "helen@xxx.com",
    ///        "password": "1234",
    ///        "client_id":"1234",
    ///        "client_secret":"xxxx"
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Produces("application/json")]
    [Route("security/test/oauth/token")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<AuthResultDto> Login([FromBody] AuthRequest authRequest)
    {
      var authResponse = await _securityService.LoginAsync(authRequest.ClientId, authRequest.Secret, authRequest.UserName, authRequest.UserPassword);
      return authResponse;
    }

    /// <summary>
    /// Redirects the user to the SAML endpoint of the identify server
    /// </summary>
    [HttpGet("security/samlp/{clientId}")]
    [ProducesResponseType(302)]
    [SwaggerOperation(Tags = new[] { "security" })]
    public IActionResult Samlp(string clientId)
    {
      var url = _securityService.GetSAMLEndpoint(clientId);
      return Redirect(url);
    }

    /// <summary>
    /// Redirects the user to the configured identity and access management login URL
    /// </summary>
    [HttpGet("security/authorize")]
    [ProducesResponseType(302)]
    [SwaggerOperation(Tags = new[] { "security" })]
    public async Task<IActionResult> Authorize(string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge,
      string prompt, string state, string nonce, string display, string login_hint, int? max_age, string acr_values)
    {
      // At the moment Security Api only supports Authorisation code flow
      if (!string.IsNullOrEmpty(response_type) && response_type != "code")
      {
        var errorUrl = redirect_uri + "?error=request_not_supported&error_description=response_type not supported";
        return Redirect(errorUrl);
      }

      var (sid, opbs) = await GenerateCookiesAsync(client_id);

      var url = await _securityService.GetAuthenticationEndPointAsync(sid, scope, response_type, client_id, redirect_uri,
        code_challenge_method, code_challenge, prompt, state, nonce, display, login_hint, max_age, acr_values);
      return Redirect(url);
    }

    /// <summary>
    /// Issues new security tokens when provide any type of following (auth code, refresh token)
    /// </summary>
    /// client_id     - REQUIRED. Client Identifier valid at the Sercurity Service.
    /// client_secret - REQUIRED for Web applications but NOT for single page applications
    /// grant_type    - REQUIRED (refresh_token | authorization_code)
    /// code          - REQUIRED when grant_type = authorization_code
    /// refresh_token - REQUIRED when grant_type = refresh_token
    /// redirect_uri  - REQUIRED (Redirection URI to which the response will be sent)
    /// state         - RECOMMENDED. Opaque value used to maintain state between the request and the callback
    /// code_verifier - Code verifier when use Authorization code flow with PKCE. 
    /// <response code="200">When grant type is "authorization_code" returns id token,refresh token and access token.When grant type is "refresh_token" returns id token and access token</response>
    /// <response  code="404">User not found</response>
    /// <response  code="401">User does not have permissions for the client</response>
    /// <response  code="400">
    /// error: invalid_request (The request is missing a required parameter, includes an unsupported parameter value(other than grant type), repeats a parameter, includes multiple credentials, utilizes more than one mechanism for authenticating the client, or is otherwise malformed
    /// error: invalid_client (Client authentication failed (e.g., unknown client, no client authentication included, or unsupported authentication method))
    /// error: invalid_grant (The provided authorization grant (e.g., authorization code, resource owner credentials) or refresh token is invalid, expired, revoked, does not match the redirection URI used in the authorization request, or was issued to another client)
    /// error: unauthorized_client (The authenticated client is not authorized to use this authorization grant type)
    /// error: unsupported_grant_type (The authorization grant type is not supported by the authorization server)
    /// "INVALID_CONNECTION" User is not allowed to sign-in using the provider
    /// </response>
    /// <remarks>
    /// Sample requests:
    /// POST client_id=abdgt refreshtoken=abcs123 granttype=refresh_token client_secret=xxx redirect_uri=http://redirect_url state=123"
    /// </remarks>
    [HttpPost("security/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<TokenResponseInfo> Token([FromForm] TokenRequest tokenRequest)
    {
      var tokenRequestInfo = new TokenRequestInfo()
      {
        ClientId = tokenRequest.ClientId,
        ClientSecret = tokenRequest.ClientSecret,
        Code = tokenRequest.Code,
        CodeVerifier = tokenRequest.CodeVerifier,
        GrantType = tokenRequest.GrantType,
        RedirectUrl = tokenRequest.RedirectUrl,
        RefreshToken = tokenRequest.RefreshToken,
        State = tokenRequest.State,
        Audience = tokenRequest.Audience
      };
      // Sessions are handled in two places for a user and they are as Auth0 & Security api (aka CCS-SSO session cookie).
      // Auth0 session is given the highest priority as it used to generate tokens. Hence, CCS-SSO session will be
      // extented for valid Auth0 sessions.
      var host = string.Empty;
      string sid = string.Empty;
      string opbsValue = string.Empty;

      if (tokenRequest.GrantType != "client_credentials")
      {
        (sid, opbsValue) = await GenerateCookiesAsync(tokenRequestInfo.ClientId, tokenRequestInfo.State);
        var redirectUri = new Uri(tokenRequestInfo.RedirectUrl);
        host = redirectUri.AbsoluteUri.Split(redirectUri.AbsolutePath)[0];
      }
      var idToken = await _securityService.GetRenewedTokenAsync(tokenRequestInfo, opbsValue, host, sid);
      return idToken;
    }

    /// <summary>
    /// Returns OP IFrame
    /// </summary>
    /// <response code="200">Returns OPIFrame successfully</response>
    [HttpGet("security/sessions")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ContentResult> CheckSession(string origin)
    {
      var path = "./Static/OPIFrame.html";
      var fileContents = await System.IO.File.ReadAllTextAsync(path);
      Response.Headers.Add(
            "Content-Security-Policy",
            "frame-ancestors " + origin);
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
    /// Code: ERROR_PASSWORD_TOO_WEAK (Password too weak)
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
    [HttpPost("security/users")]
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
    [HttpGet("security/external-idps")]
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
    ///     PUT /users
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
    [HttpPut("security/users")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task UpdateUser(UserInfo userInfo)
    {
      await _userManagerService.UpdateUserAsync(userInfo);
    }

    [HttpPost("security/users/mfa")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task UpdateUserMfaFlag(UserInfo userInfo)
    {
      await _userManagerService.UpdateUserMfaFlagAsync(userInfo);
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
    ///     POST /users/passwords
    ///     {
    ///        "UserId":"123",
    ///        "AccessToken": "1234",
    ///        "NewPassword": "newpassword",
    ///        "OldPassword":"oldpassword"
    ///     }
    /// </remarks>
    [HttpPost("security/users/passwords")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task ChangePassword(ChangePasswordDto changePasswordDto)
    {
      await _securityService.ChangePasswordAsync(changePasswordDto);
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
    /// POST /password-reset-requests
    /// {
    ///   "helen@xxx.com"
    /// }
    /// </remarks>

    [HttpPost("security/password-reset-requests")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task InitiateResetPassword(ChangePasswordInitiateRequest changePasswordInitiateRequest)
    {
      await _securityService.InitiateResetPasswordAsync(changePasswordInitiateRequest);
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
    [HttpGet("security/log-out")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(302)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LogOut([FromQuery(Name = "client-id")] string clientId, [FromQuery(Name = "redirect-uri")] string redirecturi)
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
          await _securityService.PerformBackChannelLogoutAsync(clientId, sid, visitedSiteList);
          Response.Cookies.Delete(visitedSiteCookie);
        }
      }

      return Redirect(url);
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
    /// POST security/tokens/validation?client-id=xxx Authorization: Bearer vF9dft4qmT
    /// </remarks>
    [HttpPost("security/tokens/validation")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public bool ValidateToken([FromQuery(Name = "client-id")] string clientId)
    {
      if (Request.Headers.ContainsKey("Authorization"))
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
    [HttpDelete("security/users")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task DeleteUser(string email)
    {
      await _userManagerService.DeleteUserAsync(email);
    }

    /// <summary>
    /// Reset Mfa by ticket
    /// </summary>
    /// <response code="204">Successfully reset the mfa</response>
    /// <response code="404">User not found </response>
    /// <response  code="400">
    /// Code: INVALID_TICKET (Invalid ticket)
    /// Code: MFA_RESET_FAILED (MFA reset failed)
    /// </response>
    [HttpPost("security/mfa-reset-tickets")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(500)]
    public async Task ResetMfaByTicket(MfaResetInfo mfaResetInfo)
    {
      await _userManagerService.ResetMfaAsync(mfaResetInfo.Ticket, string.Empty);
    }

    /// <summary>
    /// Send Reset Mfa email
    /// </summary>
    /// <response code="204">Successfully send the reset mfa user</response>
    [HttpPost("security/mfa-reset-notifications")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(500)]
    public async Task SendMfaEamil(MfaResetRequest mfaResetInfo)
    {
      await _userManagerService.SendResetMfaNotificationAsync(mfaResetInfo);
    }

    /// <summary>
    /// Get a user
    /// </summary>
    /// <response code="200">User details</response>
    /// <response code="404">User not found </response>
    /// <response  code="400">
    /// Code: INVALID_EMAIL (Invalid Email address)
    /// </response>
    [HttpGet("security/users")]
    [SwaggerOperation(Tags = new[] { "security" })]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IdamUser> GetUser(string email)
    {
      return await _userManagerService.GetUserAsync(email);
    }

    [HttpPost("security/users/activation-emails")]
    [Consumes("application/x-www-form-urlencoded")]
    [SwaggerOperation(Tags = new[] { "security" })]
    public async Task SendUserActivationEmail(IFormCollection userDetails, [FromQuery(Name = "is-expired")] bool isExpired = false)
    {
      userDetails.TryGetValue("email", out StringValues email);
      await _userManagerService.SendUserActivationEmailAsync(email, isExpired);
    }

    private async Task<(string, string)> GenerateCookiesAsync(string clientId, string state = null)
    {
      clientId = clientId ?? string.Empty;
      string opbsCookieName = "opbs";
      DateTime expiresOnUTC = DateTime.UtcNow.AddMinutes(_applicationConfigurationInfo.SessionConfig.SessionTimeoutInMinutes);


      CookieOptions opbsCookieOptions = new CookieOptions()
      {
        Expires = expiresOnUTC,
        Secure = true
      };

      if (!string.IsNullOrEmpty(_applicationConfigurationInfo.CustomDomain))
      {
        opbsCookieOptions.SameSite = SameSiteMode.Lax;
        opbsCookieOptions.Domain = _applicationConfigurationInfo.CustomDomain;
      }
      else
      {
        opbsCookieOptions.SameSite = SameSiteMode.None;
      }

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


      CookieOptions httpCookieOptions = new CookieOptions()
      {
        HttpOnly = true,
        Expires = expiresOnUTC,
        Secure = true
      };

      if (!string.IsNullOrEmpty(_applicationConfigurationInfo.CustomDomain))
      {
        httpCookieOptions.SameSite = SameSiteMode.Lax;
        httpCookieOptions.Domain = _applicationConfigurationInfo.CustomDomain;
      }
      else
      {
        httpCookieOptions.SameSite = SameSiteMode.None;
      }

      string sessionCookie = "ccs-sso";

      string sid;
      if (!Request.Cookies.ContainsKey(sessionCookie) && string.IsNullOrEmpty(state))
      {
        sid = Guid.NewGuid().ToString();
        var sidEncrypted = _cryptographyService.EncryptString(sid, _applicationConfigurationInfo.CryptoSettings.CookieEncryptionKey);
        Response.Cookies.Append(sessionCookie, sidEncrypted, httpCookieOptions);
      }
      else
      {
        if (Request.Cookies.ContainsKey(sessionCookie))
        {
          Request.Cookies.TryGetValue(sessionCookie, out sid);
        }
        else
        {
          var sidCache = await _securityCacheService.GetValueAsync<string>(state);
          sid = _cryptographyService.EncryptString(sidCache, _applicationConfigurationInfo.CryptoSettings.CookieEncryptionKey);
        }
        //Re-assign the same session id with new expiration time
        Response.Cookies.Delete(sessionCookie);
        Response.Cookies.Append(sessionCookie, sid, httpCookieOptions);
      }

      CookieOptions visitedSiteCookieOptions = new CookieOptions()
      {
        HttpOnly = true,
        Expires = expiresOnUTC,
        Secure = true
      };

      if (!string.IsNullOrEmpty(_applicationConfigurationInfo.CustomDomain))
      {
        visitedSiteCookieOptions.SameSite = SameSiteMode.Lax;
        visitedSiteCookieOptions.Domain = _applicationConfigurationInfo.CustomDomain;
      }
      else
      {
        visitedSiteCookieOptions.SameSite = SameSiteMode.None;
      }

      string visitedSiteCookie = "ccs-sso-visitedsites";
      if (!Request.Cookies.ContainsKey(visitedSiteCookie))
      {
        Response.Cookies.Append(visitedSiteCookie, clientId, httpCookieOptions);
      }
      else
      {
        Request.Cookies.TryGetValue(visitedSiteCookie, out string visitedSites);
        var visitedSiteList = visitedSites.Split(',').ToList();
        if (!visitedSiteList.Contains(clientId))
        {
          visitedSiteList.Add(clientId);
          visitedSites = string.Join(",", visitedSiteList);
          Response.Cookies.Delete(visitedSiteCookie);
          Response.Cookies.Append(visitedSiteCookie, visitedSites, visitedSiteCookieOptions);
        }
      }

      return (sid, opbsValue);
    }
  }
}
