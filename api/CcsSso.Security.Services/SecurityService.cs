using CcsSso.Security.DbPersistence;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
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

namespace CcsSso.Security.Services
{
  public class SecurityService : ISecurityService
  {

    private readonly IIdentityProviderService _identityProviderService;
    private readonly IJwtTokenHandler _jwtTokenHandler;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataContext _dataContext;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    public SecurityService(IIdentityProviderService awsIdentityProviderService, IJwtTokenHandler jwtTokenHandler,
      IHttpClientFactory httpClientFactory, IDataContext dataContext, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _identityProviderService = awsIdentityProviderService;
      _jwtTokenHandler = jwtTokenHandler;
      _httpClientFactory = httpClientFactory;
      _dataContext = dataContext;
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    public async Task<AuthResultDto> LoginAsync(string userName, string userPassword)
    {
      var result = await _identityProviderService.AuthenticateAsync(userName, userPassword);
      return result;
    }

    public async Task<TokenResponseInfo> GetRenewedTokenAsync(TokenRequestInfo tokenRequestInfo, string opbsValue, string host, string sid)
    {
      if (string.IsNullOrEmpty(tokenRequestInfo.Code) && string.IsNullOrEmpty(tokenRequestInfo.GrantType))
      {
        throw new CcsSsoException("INVALID_TOKEN");
      }
      TokenResponseInfo tokenResponseInfo;
      if (tokenRequestInfo.GrantType == "authorization_code")
      {
        if (string.IsNullOrEmpty(tokenRequestInfo.Code))
        {
          throw new CcsSsoException("CODE_REQUIRED");
        }

        Random rnd = new Random();
        var salt = rnd.Next(1, 6);
        // Generate Session_state and attach to the response
        var sessionState = tokenRequestInfo.ClientId + " " + host + " " + opbsValue + " " + salt;
        var sessionHash = CryptoProvider.GenerateSaltedHash(sessionState);
        var sessionStateHashWithSalt = sessionHash + "." + salt;
        tokenResponseInfo = await _identityProviderService.GetTokensAsync(tokenRequestInfo, sid);

        tokenResponseInfo.SessionState = sessionStateHashWithSalt;
      }
      else if (tokenRequestInfo.GrantType == "refresh_token")
      {
        if (string.IsNullOrEmpty(tokenRequestInfo.RefreshToken))
        {
          throw new CcsSsoException("REFRESH_TOKEN_REQUIRED");
        }
        tokenResponseInfo = await _identityProviderService.GetRenewedTokensAsync(tokenRequestInfo.ClientId, tokenRequestInfo.RefreshToken);
      }
      else
      {
        throw new CcsSsoException("UNSUPPORTED_GRANT_TYPE");
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
      if (string.IsNullOrEmpty(changePassword.NewPassword))
      {
        throw new CcsSsoException("NEW_PASSWORD_REQUIRED");
      }
      if (string.IsNullOrEmpty(changePassword.OldPassword))
      {
        throw new CcsSsoException("OLD_PASSWORD_REQUIRED");
      }
      await _identityProviderService.ChangePasswordAsync(changePassword);
    }

    public async Task<AuthResultDto> ChangePasswordWhenPasswordChallengeAsync(PasswordChallengeDto passwordChallengeDto)
    {
      if (string.IsNullOrEmpty(passwordChallengeDto.UserName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }
      if (string.IsNullOrEmpty(passwordChallengeDto.NewPassword))
      {
        throw new CcsSsoException("NEW_PASSWORD_REQUIRED");
      }
      if (string.IsNullOrEmpty(passwordChallengeDto.SessionId))
      {
        throw new CcsSsoException("SESSION_ID_REQUIRED");
      }
      return await _identityProviderService.RespondToNewPasswordRequiredAsync(passwordChallengeDto);
    }

    public async Task InitiateResetPasswordAsync(string userName)
    {
      if (string.IsNullOrEmpty(userName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }
      await _identityProviderService.InitiateResetPasswordAsync(userName);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto resetPassword)
    {
      if (string.IsNullOrEmpty(resetPassword.VerificationCode))
      {
        throw new CcsSsoException("VERIFICATION_CODE_REQUIRED");
      }
      if (string.IsNullOrEmpty(resetPassword.UserName))
      {
        throw new CcsSsoException("USERNAME_REQUIRED");
      }
      if (string.IsNullOrEmpty(resetPassword.NewPassword))
      {
        throw new CcsSsoException("NEW_PASSWORD_REQUIRED");
      }
      await _identityProviderService.ResetPasswordAsync(resetPassword);
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

    public async Task<List<string>> PerformBackChannelLogoutAsync(string sid, List<string> relyingParties)
    {
      //Temporary implementation to initiate backchannel logout     
      var relyingPartiesDB = await _dataContext.RelyingParties.Where(rp => !string.IsNullOrEmpty(rp.BackChannelLogoutUrl)
                                   && relyingParties.Contains(rp.ClientId) && !rp.IsDeleted).Select(r => r).ToListAsync();
      var successRps = new List<string>();
      foreach (var rp in relyingPartiesDB)
      {
        try
        {
          var claims = new List<KeyValuePair<string, string>>();
          claims.Add(new KeyValuePair<string, string>("sid", sid));
          // Indicate this as a logout token.
          claims.Add(new KeyValuePair<string, string>("events", "{http://schemas.openid.net/event/backchannel-logout}"));
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

    public string GetAuthenticationEndPoint(string scope, string response_type, string client_id, string redirect_uri, string code_challenge_method, string code_challenge, string prompt)
    {
      if (string.IsNullOrEmpty(client_id))
      {
        throw new CcsSsoException("CLIENT_ID_REQUIRED");
      }
      if (string.IsNullOrEmpty(response_type))
      {
        throw new CcsSsoException("RESPONSE_TYPE_REQUIRED");
      }
      if (string.IsNullOrEmpty(redirect_uri))
      {
        throw new CcsSsoException("REDIRECT_URI_REQUIRED");
      }
      if (string.IsNullOrEmpty(scope))
      {
        throw new CcsSsoException("SCOPE_REQUIRED");
      }
      return _identityProviderService.GetAuthenticationEndPoint(scope, response_type, client_id, redirect_uri, code_challenge_method, code_challenge, prompt);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
      if (string.IsNullOrEmpty(refreshToken))
      {
        throw new CcsSsoException("REFRESH_TOKEN_REQUIRED");
      }
      await _identityProviderService.RevokeTokenAsync(refreshToken);
    }

    public string GetJsonWebKeyTokens()
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
        JsonWebKey jsonWebKey = new JsonWebKey()
        {
          Kid = Base64UrlEncoder.Encode(hashBytes),
          Kty = "RSA",
          E = e,
          N = n
        };
        JsonWebKeySet jsonWebKeySet = new JsonWebKeySet();
        jsonWebKeySet.Keys.Add(jsonWebKey);
        string jsonKeys = JsonConvert.SerializeObject(jsonWebKeySet);
        return jsonKeys;
      }
    }

    public bool ValidateToken(string clientId, string token)
    {
      if(string.IsNullOrEmpty(token))
      {
        throw new CcsSsoException("TOKEN_REQUIRED");
      }

      if (string.IsNullOrEmpty(clientId))
      {
        throw new CcsSsoException("CLIENTID_REQUIRED");
      }

      var jsonKeys = GetJsonWebKeyTokens();
      var jwks = new JsonWebKeySet(jsonKeys);
      var jwk = jwks.Keys.First();
      var validationParameters = new TokenValidationParameters
      {
        IssuerSigningKey = jwk,
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

  }
}
