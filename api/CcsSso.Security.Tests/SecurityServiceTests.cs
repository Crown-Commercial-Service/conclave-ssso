using CcsSso.Security.DbPersistence;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services;
using CcsSso.Security.Tests.Helpers;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Security.Tests
{

  public class SecurityServiceTests
  {

    public class Login
    {
      [Fact]
      public async Task ReturnsToken_WhenLogin()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new AuthResultDto { IdToken = "idToken" });
        var service = GetSecurityService(mockIdentityProviderService);
        var result = await service.LoginAsync("clientId", "secret", "username", "userpwd");
        Assert.Equal("idToken", result.IdToken);
      }
    }

    public class GetRenewedToken
    {
      [Fact]
      public async Task ThrowsException_WhenPassEmptyRefreshToken()
      {
        var service = GetSecurityService();
        var tokenRequest = new TokenRequestInfo()
        {
          Code = "123"
        };
        await Assert.ThrowsAsync<SecurityException>(async () => await service.GetRenewedTokenAsync(tokenRequest, string.Empty, string.Empty, string.Empty));
      }

      [Fact]
      public async Task ReturnsToken_WhenPassValidRefreshToken()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.GetTokensAsync(It.IsAny<TokenRequestInfo>(), It.IsAny<string>())).ReturnsAsync(new TokenResponseInfo() { IdToken = "123" });
        var service = GetSecurityService(mockIdentityProviderService);
        var tokenRequest = new TokenRequestInfo()
        {
          Code = "123",
          GrantType = "authorization_code"
        };

        var result = await service.GetRenewedTokenAsync(tokenRequest, string.Empty, string.Empty, string.Empty);
        Assert.Equal("123", result.IdToken);
      }
    }

    public class GetIdentityProvidersList
    {
      [Fact]
      public async Task ReturnsIdentityProviderList()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.ListIdentityProvidersAsync())
          .ReturnsAsync(new List<IdentityProviderInfoDto>());
        var service = GetSecurityService(mockIdentityProviderService);
        var result = await service.GetIdentityProvidersListAsync();
        Assert.NotNull(result);
      }
    }

    public class ChangePassword
    {
      [Fact]
      public async Task ExecutesSuccessfully_WhenProvidedRequiredParameters()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        var service = GetSecurityService(mockIdentityProviderService);
        var changePasswordRequest = new ChangePasswordDto()
        {
          UserName = "123",
          NewPassword = "Monday@123",
          OldPassword = "def"
        };
        await service.ChangePasswordAsync(changePasswordRequest);
        mockIdentityProviderService.Verify(p => p.ChangePasswordAsync(changePasswordRequest));
      }

      public static IEnumerable<object[]> InvalidData =>
              new List<object[]>
              {
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("", "", ""),
                        "USER_NAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "", ""),
                        "NEW_PASSWORD_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "Monday@12345", ""),
                        "OLD_PASSWORD_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "monday@12345", "Monday@12345"),
                        "ERROR_PASSWORD_TOO_WEAK"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "MONDAY@12345", "Monday@12345"),
                        "ERROR_PASSWORD_TOO_WEAK"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "Monday12345", "Monday@12345"),
                        "ERROR_PASSWORD_TOO_WEAK"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "Monday@OneTwo", "Monday@12345"),
                        "ERROR_PASSWORD_TOO_WEAK"
                    },
                new object[]
                    {
                        DtoHelper.GetChangePasswordDto("test@yopmail.com", "Mon@12345", "Monday@12345"),
                        "ERROR_PASSWORD_TOO_WEAK"
                    },
              };

      [Theory]
      [MemberData(nameof(InvalidData))]
      public async Task ThrowsException_WhenMandatoryFieldsAreNotProvided(ChangePasswordDto changePasswordDto, string errorCode)
      {
        var service = GetSecurityService();
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.ChangePasswordAsync(changePasswordDto));
        Assert.Equal(errorCode, ex.Message);
      }
    }

    public class InitiateResetPassword
    {
      [Fact]
      public async Task ExecutesSuccessfully_WhenProvidedRequiredParameters()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        var service = GetSecurityService(mockIdentityProviderService);
        var userName = "smith@gmail.com";
        ChangePasswordInitiateRequest changePasswordInitiateRequest = new ChangePasswordInitiateRequest()
        {
          UserName = userName
        };
        await service.InitiateResetPasswordAsync(changePasswordInitiateRequest);
        mockIdentityProviderService.Verify(p => p.InitiateResetPasswordAsync(changePasswordInitiateRequest));
      }

      [Fact]
      public async Task ThrowsException_WhenMandatoryFieldsAreNotProvided()
      {
        var service = GetSecurityService();
        ChangePasswordInitiateRequest changePasswordInitiateRequest = new ChangePasswordInitiateRequest()
        {
          UserName = string.Empty
        };
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.InitiateResetPasswordAsync(changePasswordInitiateRequest));
        Assert.Equal("USERNAME_REQUIRED", ex.Message);
      }
    }

    public class PerformBackChannelLogout
    {
      [Fact]
      public async Task PerformsBackChannelLogoutFromRPsWhichSupportBackChanelLogout_WhenProvidedAListOfRps()
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          var mockHttpClientFactory = new Mock<IHttpClientFactory>();
          var mockHttpClient = new Mock<HttpClient>();
          mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);
          await SetupTestDataAsync(dataContext);
          var service = GetSecurityService(null, dataContext, mockHttpClientFactory);
          var list = await service.PerformBackChannelLogoutAsync("123","123", new List<string>() { "1", "2", "3", "4" });
          Assert.Equal(2, list.Count());
          Assert.Equal(new List<string>(){ "1", "2"}, list);
        });
      }
    }

    public class GetJsonWebKeyTokens
    {
      [Fact]
      public void GeneratesJsonWebKeyTokens()
      {
        var service = GetSecurityService();
        var result = service.GetJsonWebKeyTokens();
        Assert.NotNull(result);
      }
    }

    public class ValidateToken
    {
      public static IEnumerable<object[]> InvalidData =>
             new List<object[]>
             {
                    new object[]
                    {
                        "123",
                        "",
                        "TOKEN_REQUIRED"
                    },
                    new object[]
                    {
                        "",
                        "123",
                        "CLIENTID_REQUIRED"
                    }
             };

      [Theory]
      [MemberData(nameof(InvalidData))]
      public void CheckErrorValidation(string clienId, string token, string error)
      {
        var service = GetSecurityService();
        var ex = Assert.Throws<CcsSsoException>(() => service.ValidateToken(clienId, token));
        Assert.Equal(error, ex.Message);
      }
    }

    public class Logout
    {
      [Fact]
      public async Task ExecutesSuccessfully_WhenProvidedRequiredParameters()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        var service = GetSecurityService(mockIdentityProviderService);
        var userName = "smith@gmail.com";
        string clientId = "123";
        await service.LogoutAsync(clientId, userName);
        mockIdentityProviderService.Verify(p => p.SignOutAsync(clientId, userName));
      }

      [Fact]
      public async Task ThrowsException_WhenMandatoryFieldsAreNotProvided()
      {
        var service = GetSecurityService();
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.LogoutAsync(string.Empty, string.Empty));
        Assert.Equal("REDIRECT_URI_REQUIRED", ex.Message);
      }
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.RelyingParties.Add(new DbPersistence.Entities.RelyingParty { Id = 1, Name = "1", ClientId = "1", BackChannelLogoutUrl = "http://localhost:5000" });
      dataContext.RelyingParties.Add(new DbPersistence.Entities.RelyingParty { Id = 2, Name = "2", ClientId = "2", BackChannelLogoutUrl = "http://localhost:5001" });
      dataContext.RelyingParties.Add(new DbPersistence.Entities.RelyingParty { Id = 3, Name = "3", ClientId = "3", BackChannelLogoutUrl = null });
      dataContext.RelyingParties.Add(new DbPersistence.Entities.RelyingParty { Id = 4, Name = "4", ClientId = "4", BackChannelLogoutUrl = null });
      dataContext.RelyingParties.Add(new DbPersistence.Entities.RelyingParty { Id = 5, Name = "5", ClientId = "5", BackChannelLogoutUrl = "http://localhost:5003", IsDeleted = true });
      dataContext.RelyingParties.Add(new DbPersistence.Entities.RelyingParty { Id = 6, Name = "6", ClientId = "6", BackChannelLogoutUrl = "http://localhost:5004", });
      await dataContext.SaveChangesAsync();
    }

    public static SecurityService GetSecurityService(Mock<IIdentityProviderService> mockIdentityProviderService = null, IDataContext mockIDataContext = null,
      Mock<IHttpClientFactory> mockHttpClientFactory = null)
    {
      if (mockIdentityProviderService == null)
      {
        mockIdentityProviderService = new Mock<IIdentityProviderService>();
      }

      if (mockHttpClientFactory == null)
      {
        mockHttpClientFactory = new Mock<IHttpClientFactory>();
      }

      // This is just a sample test RSA cryptograpy key which does not use
      var applicationConfigurationInfo = new ApplicationConfigurationInfo()
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
XfpE78ZNmRoLpF5k61uHRBafBlKloM73jyoZwQtBFfPqptFLwbw=",
          RsaPublicKey = @"-----BEGIN PUBLIC KEY-----
                            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAug6P3se47y3waJ5yS4JN
                            hDEmUOjQjhee+hrYIkeKlGP0VTXuA72oI34QojPa+WBQxt3sO8otz0OaaR1UlO7m
                            LVLKuWkZme6kSv5znSuUBujDcV1hvsKmRuoXYL6gLQKpTrQWwQPCn6yS5MQbdP+L
                            V5umQbehGcGo9v4GQVnEaXK+CwwMWmQU1UBxmDbM4RBC1Kf56+L08NyjdPZtL20p
                            L2spxlSdSQrrNVYZLw0WBvevZBtD6J7oNlGmq8iDMoMPYr0MM3MNei32C4XHztoe
                            GvsvR8SN542b3RzsbmCm3bM7hI0bdFRNbvW6w5Y8fK6nz+nB6yJRsi8Dem+GKCsU
                            ZQIDAQAB
                            -----END PUBLIC KEY-----
                            "
        },
        PasswordPolicy = new PasswordPolicy
        {
          LowerAndUpperCaseWithDigits = true,
          RequiredLength = 10
        }
      };

      var jwtTokenHandler = new JwtTokenHandler(applicationConfigurationInfo);
      Mock<ICcsSsoEmailService> mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
      var mockSecurityCacheService = new Mock<ISecurityCacheService>();
      var service = new SecurityService(mockIdentityProviderService.Object, jwtTokenHandler, mockHttpClientFactory.Object,
        mockIDataContext, applicationConfigurationInfo, mockCcsSsoEmailService.Object, mockSecurityCacheService.Object);
      return service;
    }
  }
}
