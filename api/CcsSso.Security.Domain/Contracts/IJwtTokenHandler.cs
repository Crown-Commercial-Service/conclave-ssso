using CcsSso.Security.Domain.Dtos;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace CcsSso.Security.Domain.Contracts
{
  public interface IJwtTokenHandler
  {
    string CreateToken(string audience, List<KeyValuePair<string, string>> customClaims, int tokenExpirationTimeInMinutes);

    JwtSecurityToken DecodeToken(string token);
  }
}
