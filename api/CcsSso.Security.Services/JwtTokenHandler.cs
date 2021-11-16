using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Shared.Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CcsSso.Security.Services
{
  public class JwtTokenHandler : IJwtTokenHandler
  {
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    public JwtTokenHandler(ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    public JwtSecurityToken DecodeToken(string token)
    {
      try
      {
        var tokenStream = new JwtSecurityTokenHandler();
        var tokenDecoded = tokenStream.ReadToken(token) as JwtSecurityToken;
        return tokenDecoded;
      }
      catch (Exception)
      {
        throw new CcsSsoException("INVALID_TOKEN");
      }
    }

    public string CreateToken(string audience, List<ClaimInfo> customClaims, int tokenExpirationTimeInMinutes)
    {
      var privateKey = _applicationConfigurationInfo.JwtTokenConfiguration.RsaPrivateKey.ToByteArray();

      using (RSA rsa = RSA.Create())
      {
        rsa.ImportRSAPrivateKey(privateKey, out int bytesRead);

        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        {
          CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var now = DateTime.UtcNow;
        var unixTimeSeconds = new DateTimeOffset(now).ToUnixTimeSeconds();

        var claims = new List<Claim>();
        claims.Add(new Claim(JwtRegisteredClaimNames.Iat, unixTimeSeconds.ToString(), ClaimValueTypes.Integer64));
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        foreach (var tuple in customClaims)
        {
          claims.Add(new Claim(tuple.Key, tuple.Value ?? string.Empty));
        }

        var jwt = new JwtSecurityToken(
            audience: audience,
            issuer: _applicationConfigurationInfo.JwtTokenConfiguration.Issuer,
            claims: claims,
            expires: now.AddMinutes(tokenExpirationTimeInMinutes),
            signingCredentials: signingCredentials
        );
        string token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return token;
      }
    }
  }
}
