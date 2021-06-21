using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Security.Tests
{
  public class JwtTokenHandlerTests
  {
    public class CreateToken
    {
      [Fact]
      public void GeneratesJsonWebKeyTokens()
      {
        var service = GetJwtTokenHandler();
        var result = service.CreateToken("brickendon", new List<ClaimInfo>() {
        new ClaimInfo("name","Ann"),
        new ClaimInfo("email","Ann@brickendon.com")
        }, 10);
        Assert.NotNull(result);
        var tokenDecoded = service.DecodeToken(result);
        var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        Assert.Equal("Ann@brickendon.com", email);
        var name = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        Assert.Equal("Ann", name);
      }
    }

    public class DecodeToken
    {
      [Fact]
      public void DecodeToken_WhenProvideValidToken()
      {
        var service = GetJwtTokenHandler();
        var result = service.CreateToken("brickendon", new List<ClaimInfo>() {
        new ClaimInfo("name","Ann"),
        new ClaimInfo("email","Ann@brickendon.com")
        }, 10);
        Assert.NotNull(result);
        var tokenDecoded = service.DecodeToken(result);
        Assert.NotNull(tokenDecoded);
        Assert.Equal(6, tokenDecoded.Claims.Count());
        var email = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        Assert.Equal("Ann@brickendon.com", email);
        var name = tokenDecoded.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        Assert.Equal("Ann", name);
      }

      [Fact]
      public void FaiedToDecodeToken_WhenProvideInValidToken()
      {
        var service = GetJwtTokenHandler();
        var ex = Assert.Throws<CcsSsoException>(()=>service.DecodeToken("123"));
        Assert.Equal("INVALID_TOKEN", ex.Message);
      }
    }

    public static JwtTokenHandler GetJwtTokenHandler()
    {
      // Following is just a TEST RSASHA256 cryptographic private key
      var applicationConfig = new ApplicationConfigurationInfo()
      {
        JwtTokenConfiguration = new JwtTokenConfiguration()
        {
          RsaPrivateKey = @"MIIEogIBAAKCAQBrTHrgmqeLnvxmaKFWXODPxPVyKq1rjwm1rW/EmckT6vcNjLkd
HcHlIjHlxfsZ9dRVHrklDDrdUYrjyKVULJpmMaefxqRsUZabhHSfN2wDO3uc+QQF
S+vmZUQVSiQs5rIODt3chKInV8K2HGMGVk6ifLq9ozltg1IWtzeGQ+JxC+TL85yM
zHIgZvkuf76zN1GvLGSajamJjpkeQhjuBE0w/LDb+UNFfbEwD7i/Ltd9h17iE3y9
OiyYHgGBt2Rr8m75N1tJv/HVtC27QtKdUqx+vUGYxh0I0ktZJOQkHL+iySwWgu/h
F8+vew77Izy4gzpSLUXtgiz8dT3kxHYey1GhAgMBAAECggEABCHh6ZyLL1lkJx2I
eScCkX3oZgk2vJm5qgGP+GZj1ByMfz0YNALdYNG8UjkZvpo1H0Ibp02dRsDJNJSZ
qXA+Ugk/h2vDEVjjEAI965Pa2RUFYbpFaV7PKwRjZt6AHiqUWO5BpSiGhjVfDlxx
g+D3DlL3bi5HG+ye0LklrkoXAnuBmufoLwQGiQ748z/r3TRjkdMwgN3FOAjlXt6E
bXG2XKnpMInfzYfRssvgfZOkA8s2t92UN5XHb0CfZTT2qRuBohWVsXDWuS8cy/iY
iOPB/D+4PYFHL/Z5yL/rzM564H/DTv8W7lX0+3gcEtl8+caJQS1Ug4vMk+cH64t5
5M4pQQKBgQDVeodal9lF2KPbiFxe/6Nkrq5SLtUHHivA/JXuZQqpUuNWBYMGeQcR
Jo9qer/kkiStufqTBtEbwBzYr3md8gp2m7aOc4IoXDQHvNr/vigzB6WkgBeZ4YHq
72WiyytmLoANwH00z3n/mXcvP3LmayTYfi8kanlCFP+p2OyPSd9FvQKBgQCAq73l
kLgwOjiRDLF15qvIq2uUPaVa1Q+IOz5KlvUyb1BXv9Za4ZkwM/DCJs249gMYtHDx
TYk3R/kuuKU+WtpNzsYyxIoK/mK8mnX423wipMOVQ4hOSTl63Mte495QOgCESzsW
nRbIO6KNEympJAlITDmZGyR+NJU9xodtF3O/tQKBgAioeYjhE7zTdoHW2/g2YggZ
VZSbtaQeQyQGmoYarv4DEJlSi+fdTL4TVEm7RMEedEJfgpwn8J8xgXoAU+xl/qqk
0hig8qx0YN/XdwJcUgEP1FYBo/Nnw/8lSnf5yX7Rb/wezHUx3P7S2JSf/CcAPXRS
WdMeRmC5vUzEMYP2OQn5AoGAGaekjkAjqWICY1FflL1wZOtg4MbF0G2I0kXVrrOY
ofy1zTkvuSEgFQ9WIq/v9r/+cA+SMVqfUElmcp1YPS2KN1dSB09OotCDyU0W1o6U
mqe1Y256n/lTn56kYgAXFHHsJnSFjIW3xCa3y18VwGax/xtLpK5XwV4kn5OU7vht
GL0CgYEAlPZE/b+7ovxnuBpf3lxj1cLpOOvN2qF9Maf9rFkLnOuSQn6RI56uyZE+
z56EOcILdZOQ6T9OvUL0uqGZNroCnU+j0RXumK83uNjIZUGsdpS1XrfzXLvT3Qct
XfpE78ZNmRoLpF5k61uHRBafBlKloM73jyoZwQtBFfPqptFLwbw="
        }
      };
      return new JwtTokenHandler(applicationConfig);
    }
  }
}
