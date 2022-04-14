using CcsSso.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Contracts
{
  public interface ITokenService
  {
    Task<JwtTokenValidationInfo> ValidateTokenAsync(string token, string jwksUrl, string idamClienId, string issuer, List<string> keys = null);

    Task<JwtTokenValidationInfo> ValidateTokenWithoutAudienceAsync(string token, string jwksUrl, string audience, string issuer, List<string> claims = null);
  }
}
