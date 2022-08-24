using CcsSso.Security.DbPersistence;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static CcsSso.Security.Domain.Constants.Constants;

namespace CcsSso.Security.Services
{
  public class SecurityService : ISecurityService
  {

    private readonly IIdentityProviderService _identityProviderService;
    private readonly IJwtTokenHandler _jwtTokenHandler;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataContext _dataContext;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly ISecurityCacheService _securityCacheService;
    public SecurityService(IIdentityProviderService awsIdentityProviderService, IJwtTokenHandler jwtTokenHandler,
      IHttpClientFactory httpClientFactory, IDataContext dataContext, ApplicationConfigurationInfo applicationConfigurationInfo,
      ICcsSsoEmailService ccsSsoEmailService, ISecurityCacheService securityCacheService)
    {
      _identityProviderService = awsIdentityProviderService;
      _jwtTokenHandler = jwtTokenHandler;
      _httpClientFactory = httpClientFactory;
      _dataContext = dataContext;
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _ccsSsoEmailService = ccsSsoEmailService;
      _securityCacheService = securityCacheService;
    }

    public async Task<AuthResultDto> LoginAsync(string clientId, string secret, string userName, string userPassword)
    {
      var result = await _identityProviderService.AuthenticateAsync(clientId, secret, userName, userPassword);
      return result;
    }

    public string GetSAMLEndpoint(string clientId)
    {
      return _identityProviderService.GetSAMLAuthenticationEndPoint(clientId);
    }

    public async Task<TokenResponseInfo> GetRenewedTokenAsync(TokenRequestInfo tokenRequestInfo, string opbsValue, string host, string sid, List<string> visitedSiteList = null)
    {
      TokenResponseInfo tokenResponseInfo;
      if (tokenRequestInfo.GrantType == "authorization_code")
      {
        tokenResponseInfo = await _identityProviderService.GetTokensAsync(tokenRequestInfo, sid);

        Random rnd = new Random();
        var salt = rnd.Next(1, 6);
        // Generate Session_state and attach to the response
        var sessionState = tokenRequestInfo.ClientId + " " + host + " " + opbsValue + " " + salt;
        var sessionHash = CryptoProvider.GenerateSaltedHash(sessionState);
        var sessionStateHashWithSalt = sessionHash + "." + salt;
        tokenResponseInfo.SessionState = sessionStateHashWithSalt;
      }
      else if (tokenRequestInfo.GrantType == "refresh_token")
      {
        tokenResponseInfo = await _identityProviderService.GetRenewedTokensAsync(tokenRequestInfo, sid);

        // #Delegated: To perform back channel logout while switching delegated org
        if (visitedSiteList != null && visitedSiteList.Count > 0 && !string.IsNullOrEmpty(tokenRequestInfo.DelegatedOrgId))
        {
          string sidFromToken = string.Empty;
          sidFromToken = await _identityProviderService.GetSidFromRefreshToken(tokenResponseInfo.RefreshToken, sidFromToken);
          await this.PerformBackChannelLogoutAsync(tokenRequestInfo.ClientId, sidFromToken, visitedSiteList);
        }
      }
      else if (tokenRequestInfo.GrantType == "client_credentials")
      {
        tokenResponseInfo = await _identityProviderService.GetMachineTokenAsync(tokenRequestInfo.ClientId, tokenRequestInfo.ClientSecret, tokenRequestInfo.Audience);
      }
      else
      {
        var errorInfo = new ErrorInfo()
        {
          Error = "invalid_grant"
        };
        throw new SecurityException(errorInfo);
      }
      return tokenResponseInfo;
    }

    public async Task<string> GetIdentityProviderAuthenticationEndPointAsync()
    {
      return await _identityProviderService.GetIdentityProviderAuthenticationEndPointAsync();
    }

    public async Task<List<IdentityProviderInfoDto>> GetIdentityProvidersListAsync()
    {
      var idProviders = await _identityProviderService.ListIdentityProvidersAsync();
      return idProviders;
    }

    public async Task ChangePasswordAsync(ChangePasswordDto changePassword)
    {
      if (string.IsNullOrEmpty(changePassword.UserName))
      {
        throw new CcsSsoException("USER_NAME_REQUIRED");
      }

      ValidateEmail(changePassword.UserName);

      if (string.IsNullOrEmpty(changePassword.NewPassword))
      {
        throw new CcsSsoException("NEW_PASSWORD_REQUIRED");
      }
      if (string.IsNullOrEmpty(changePassword.OldPassword))
      {
        throw new CcsSsoException("OLD_PASSWORD_REQUIRED");
      }
      if ((_applicationConfigurationInfo.PasswordPolicy.LowerAndUpperCaseWithDigits &&
        !UtilityHelper.IsPasswordValidForRequiredCharactors(changePassword.NewPassword))
        || changePassword.NewPassword.Length < _applicationConfigurationInfo.PasswordPolicy.RequiredLength)
      {
        throw new CcsSsoException("ERROR_PASSWORD_TOO_WEAK");
      }
      await _identityProviderService.ChangePasswordAsync(changePassword);

      //send notification
      await _ccsSsoEmailService.SendChangePasswordNotificationAsync(changePassword.UserName);
    }

    public async Task InitiateResetPasswordAsync(ChangePasswordInitiateRequest changePasswordInitiateRequest)
    {
      if (string.IsNullOrEmpty(changePasswordInitiateRequest.UserName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }

      ValidateEmail(changePasswordInitiateRequest.UserName);

      await _identityProviderService.InitiateResetPasswordAsync(changePasswordInitiateRequest);
    }

    public async Task<string> LogoutAsync(string clientId, string redirecturi)
    {
      if (string.IsNullOrEmpty(redirecturi))
      {
        throw new CcsSsoException("REDIRECT_URI_REQUIRED");
      }

      if (string.IsNullOrEmpty(clientId))
      {
        throw new CcsSsoException("CLIENT_ID_REQUIRED");
      }

      return await _identityProviderService.SignOutAsync(clientId, redirecturi);
    }

    public async Task<List<string>> PerformBackChannelLogoutAsync(string clientId, string sid, List<string> relyingParties)
    {
      //Temporary implementation to initiate backchannel logout     
      var relyingPartiesDB = await _dataContext.RelyingParties.Where(rp => rp.ClientId != clientId && !string.IsNullOrEmpty(rp.BackChannelLogoutUrl)
                                   && relyingParties.Contains(rp.ClientId) && !rp.IsDeleted).Select(r => r).ToListAsync();
      var successRps = new List<string>();
      foreach (var rp in relyingPartiesDB)
      {
        try
        {
          var claims = new List<ClaimInfo>();
          claims.Add(new ClaimInfo("sid", sid));
          // Indicate this as a logout token.
          claims.Add(new ClaimInfo("events", "{http://schemas.openid.net/event/backchannel-logout}"));
          var logoutToken = _jwtTokenHandler.CreateToken(rp.ClientId, claims, _applicationConfigurationInfo.JwtTokenConfiguration.IDTokenExpirationTimeInMinutes);

          var client = _httpClientFactory.CreateClient();
          client.BaseAddress = new Uri(rp.BackChannelLogoutUrl);

          var list = new List<KeyValuePair<string, string>>();
          list.Add(new KeyValuePair<string, string>("logout_token", logoutToken));
          HttpContent codeContent = new FormUrlEncodedContent(list);
          await client.PostAsync(string.Empty, codeContent);
        }
        catch (Exception)
        {
          // Handle gracefully such as logging. This should not inturupt for other RPs
        }
        successRps.Add(rp.ClientId);
      }
      return successRps;
    }

    public async Task<string> GetAuthenticationEndPointAsync(string sid, string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt, string state, string nonce, string display, string login_hint, int? max_age, string acr_values)
    {
      //Generate new state and associate with sid
      if (string.IsNullOrEmpty(state))
      {
        state = Guid.NewGuid().ToString();
      }
      await _securityCacheService.SetValueAsync(state, sid, new TimeSpan(0, _applicationConfigurationInfo.SessionConfig.StateExpirationInMinutes, 0));
      return _identityProviderService.GetAuthenticationEndPoint(state, scope, response_type, client_id, redirect_uri,
        code_challenge_method, code_challenge, prompt, nonce, display, login_hint, max_age, acr_values);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
      if (string.IsNullOrEmpty(refreshToken))
      {
        throw new CcsSsoException("REFRESH_TOKEN_REQUIRED");
      }
      await _identityProviderService.RevokeTokenAsync(refreshToken);
    }

    public JsonWebKeySetInfo GetJsonWebKeyTokens()
    {
      using (var textReader = new StringReader(_applicationConfigurationInfo.JwtTokenConfiguration.RsaPublicKey))
      {
        var pubkeyReader = new PemReader(textReader);
        RsaKeyParameters KeyParameters = (RsaKeyParameters)pubkeyReader.ReadObject();
        var e = Base64UrlEncoder.Encode(KeyParameters.Exponent.ToByteArrayUnsigned());
        var n = Base64UrlEncoder.Encode(KeyParameters.Modulus.ToByteArrayUnsigned());
        var dict = new Dictionary<string, string>() {
                    {"e", e},
                    {"kty", "RSA"},
                    {"n", n}
                };
        var hash = SHA256.Create();
        Byte[] hashBytes = hash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));
        JsonWebKeyInfo jsonWebKey = new JsonWebKeyInfo()
        {
          Kid = Base64UrlEncoder.Encode(hashBytes),
          Kty = "RSA",
          Use = "sig",
          E = e,
          N = n,
          Alg = "RS256"
        };
        JsonWebKeySetInfo jsonWebKeySet = new JsonWebKeySetInfo();
        jsonWebKeySet.Keys = new List<JsonWebKeyInfo>();
        jsonWebKeySet.Keys.Add(jsonWebKey);
        return jsonWebKeySet;
      }
    }

    public bool ValidateToken(string clientId, string token)
    {
      if (string.IsNullOrEmpty(token))
      {
        throw new CcsSsoException("TOKEN_REQUIRED");
      }

      if (string.IsNullOrEmpty(clientId))
      {
        throw new CcsSsoException("CLIENTID_REQUIRED");
      }

      var jwks = GetJsonWebKeyTokens();
      var jwk = jwks.Keys.First();
      var signingKey = new JsonWebKey()
      {
        Kty = jwk.Kty,
        E = jwk.E,
        N = jwk.N,
      };
      var validationParameters = new TokenValidationParameters
      {
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ValidAudience = clientId,
        ValidIssuer = _applicationConfigurationInfo.JwtTokenConfiguration.Issuer
      };

      var tokenHandler = new JwtSecurityTokenHandler();

      try
      {
        tokenHandler.ValidateToken(token, validationParameters, out _);
      }
      catch (Exception)
      {
        return false;
      }
      return true;
    }

    public async Task<ServiceAccessibilityResultDto> CheckServiceAccessForUserAsync(string clientId, string email)
    {
      if (string.IsNullOrEmpty(clientId))
      {
        throw new CcsSsoException("ERROR_INVALID_CLIENTID");
      }

      if (string.IsNullOrEmpty(email))
      {
        throw new CcsSsoException("ERROR_INVALID_EMAIL");
      }

      return await _identityProviderService.CheckServiceAccessForUserAsync(clientId, email);
    }

    public async Task InvalidateSessionAsync(string sessionId)
    {
      await _securityCacheService.SetValueAsync(sessionId, true, new TimeSpan(0, _applicationConfigurationInfo.SessionConfig.SessionTimeoutInMinutes, 0));
      await _securityCacheService.RemoveAsync(CacheKey.DELEGATION + sessionId);
    }

    private void ValidateEmail(string email)
    {
      if (!UtilityHelper.IsEmailFormatValid(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailFormatError);
      }

      if (!UtilityHelper.IsEmailLengthValid(email))
      {
        throw new CcsSsoException(ErrorCodes.EmailTooLongError);
      }
    }

  }
}
