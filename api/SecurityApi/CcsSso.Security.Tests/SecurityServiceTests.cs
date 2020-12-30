using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services;
using CcsSso.Security.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
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
        mockIdentityProviderService.Setup(m => m.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new AuthResultDto { IdToken = "idToken" });
        var service = GetSecurityService(mockIdentityProviderService);
        var result = await service.LoginAsync("username", "userpwd");
        Assert.Equal("idToken", result.IdToken);
      }
    }

    public class GetRenewedToken
    {
      [Fact]
      public async Task ThrowsException_WhenPassEmptyRefreshToken()
      {
        var service = GetSecurityService();
        await Assert.ThrowsAsync<CcsSsoException>(async () => await service.GetRenewedTokenAsync(string.Empty));
      }

      [Fact]
      public async Task ReturnsToken_WhenPassValidRefreshToken()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.GetRenewedTokenAsync(It.IsAny<string>())).ReturnsAsync("123");
        var service = GetSecurityService(mockIdentityProviderService);
        var result = await service.GetRenewedTokenAsync("xpo12opl2pl3pl3plplp3pop");
        Assert.Equal("123", result);
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
          AccessToken = "123",
          NewPassword = "abc",
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
                        "ACCESS_TOKEN_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("123", "", ""),
                        "NEW_PASSWORD_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetChangePasswordDto("123", "145", ""),
                        "OLD_PASSWORD_REQUIRED"
                    }
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
        await service.InitiateResetPasswordAsync(userName);
        mockIdentityProviderService.Verify(p => p.InitiateResetPasswordAsync(userName));
      }

      [Fact]
      public async Task ThrowsException_WhenMandatoryFieldsAreNotProvided()
      {
        var service = GetSecurityService();
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.InitiateResetPasswordAsync(string.Empty));
        Assert.Equal("USERNAME_REQUIRED", ex.Message);
      }
    }

    public class ResetPassword
    {
      [Fact]
      public async Task ExecutesSuccessfully_WhenProvidedRequiredParameters()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        var service = GetSecurityService(mockIdentityProviderService);
        ResetPasswordDto resetPasswordDto = new ResetPasswordDto()
        {
          UserName = "amil@yahoo.com",
          VerificationCode = "123",
          NewPassword = "abc"
        };
        await service.ResetPasswordAsync(resetPasswordDto);
        mockIdentityProviderService.Verify(p => p.ResetPasswordAsync(resetPasswordDto));
      }

      public static IEnumerable<object[]> InvalidData =>
             new List<object[]>
             {
                    new object[]
                    {
                        DtoHelper.GetResetPasswordDto("", "", ""),
                        "VERIFICATION_CODE_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetResetPasswordDto("", "123", "333"),
                        "USERNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetResetPasswordDto("123", "", "145"),
                        "NEW_PASSWORD_REQUIRED"
                    }
             };

      [Theory]
      [MemberData(nameof(InvalidData))]
      public async Task ThrowsException_WhenMandatoryFieldsAreNotProvided(ResetPasswordDto resetPasswordDto, string errorCode)
      {
        var service = GetSecurityService();
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.ResetPasswordAsync(resetPasswordDto));
        Assert.Equal(errorCode, ex.Message);
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
        await service.LogoutAsync(userName);
        mockIdentityProviderService.Verify(p => p.SignOutAsync(userName));
      }

      [Fact]
      public async Task ThrowsException_WhenMandatoryFieldsAreNotProvided()
      {
        var service = GetSecurityService();
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.LogoutAsync(string.Empty));
        Assert.Equal("USERNAME_REQUIRED", ex.Message);
      }
    }

    public static SecurityService GetSecurityService(Mock<IIdentityProviderService> mockIdentityProviderService = null)
    {
      if (mockIdentityProviderService == null)
      {
        mockIdentityProviderService = new Mock<IIdentityProviderService>();
      }
      var service = new SecurityService(mockIdentityProviderService.Object);
      return service;
    }
  }
}
