using CcsSso.Security.Domain.Constants;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services;
using CcsSso.Security.Tests.Helpers;
using CcsSso.Shared.Domain.Contexts;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static CcsSso.Security.Domain.Constants.Constants;

namespace CcsSso.Security.Tests
{

  public class UserManagerServiceTests
  {
    public class CreateUser
    {

      public static IEnumerable<object[]> ValidData =>
               new List<object[]>
               {
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("firstName", "lastName", "fl@mail.com")
                    }
               };

      [Theory]
      [MemberData(nameof(ValidData))]
      public async Task RegisterUser_WhenPassValidData(UserInfo userInfo)
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.CreateUserAsync(It.IsAny<UserInfo>())).ReturnsAsync(new UserRegisterResult());

        var service = GetUserManagerService(mockIdentityProviderService);
        var result = await service.CreateUserAsync(userInfo);
        Assert.NotNull(result);
      }

      [Theory]
      [MemberData(nameof(ValidData))]
      public async Task ThrowsException_WhenNotPassrequiredData(UserInfo userInfo)
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.CreateUserAsync(It.IsAny<UserInfo>())).ReturnsAsync(new UserRegisterResult());

        var service = GetUserManagerService(mockIdentityProviderService);
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.SendUserActivationEmailAsync(null));
        Assert.Equal(ErrorCodes.EmailRequired, ex.Message);
      }


      public static IEnumerable<object[]> InvalidData =>
              new List<object[]>
              {
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("", "lastName", "fl@mail.com"),
                        "ERROR_FIRSTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto(null, "lastName", "fl@mail.com"),
                        "ERROR_FIRSTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "", "fl@mail.com"),
                        "ERROR_LASTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", null, "fl@mail.com"),
                        "ERROR_LASTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "lastName", ""),
                        "ERROR_EMAIL_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "lastName", null),
                        "ERROR_EMAIL_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "lastName", "mail"),
                        "ERROR_EMAIL_FORMAT"
                    },
              };

      [Theory]
      [MemberData(nameof(InvalidData))]
      public async Task ThrowsException_WhenPassInvalidData(UserInfo userInfo, string expectedError)
      {
        var service = GetUserManagerService();
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.CreateUserAsync(userInfo));
        Assert.Equal(expectedError, ex.Message);
      }
    }

    public class ResetMfa
    {
      [Theory]
      [InlineData("123", Constants.ErrorCodes.InvalidTicket)]
      [InlineData(null, Constants.ErrorCodes.UserIdRequired)]
      public async Task ThrowsException_WhenInvalidTicket(string ticket, string errorCode)
      {
        var mockSecurityCacheService = new Mock<ISecurityCacheService>();
        var mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
        var applicationConfigurationInfo = new ApplicationConfigurationInfo();
        mockSecurityCacheService.Setup(m => m.GetValueAsync<string>(It.IsAny<string>())).ReturnsAsync(string.Empty);
        var service = GetUserManagerService(null, mockSecurityCacheService, applicationConfigurationInfo, mockCcsSsoEmailService);

        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.ResetMfaAsync(ticket, null));
        Assert.Equal(errorCode, ex.Message);
      }

      [Fact]
      public async Task ResetMfa_WhenProvideValidTicket()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        var mockSecurityCacheService = new Mock<ISecurityCacheService>();
        var mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
        var applicationConfigurationInfo = new ApplicationConfigurationInfo();
        mockSecurityCacheService.Setup(m => m.GetValueAsync<string>(It.IsAny<string>())).ReturnsAsync("tom@yopmail.com");
        var service = GetUserManagerService(mockIdentityProviderService, mockSecurityCacheService, applicationConfigurationInfo, mockCcsSsoEmailService);
        await service.ResetMfaAsync("1234", null);
        mockIdentityProviderService.Verify(a => a.ResetMfaAsync("tom@yopmail.com"));
        mockSecurityCacheService.Verify(a => a.RemoveAsync(Constants.CacheKey.MFA_RESET + "1234"));
      }
    }

    public class SendResetMfaNotification
    {
      [Fact]
      public async Task SendEmail_WhenProvideValidTicket()
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        var mockSecurityCacheService = new Mock<ISecurityCacheService>();
        var mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
        var applicationConfigurationInfo = new ApplicationConfigurationInfo()
        {
          MfaSetting = new MfaSetting()
        };
        mockSecurityCacheService.Setup(m => m.GetValueAsync<string>(It.IsAny<string>())).ReturnsAsync("tom@yopmail.com");
        var service = GetUserManagerService(mockIdentityProviderService, mockSecurityCacheService, applicationConfigurationInfo, mockCcsSsoEmailService);
        await service.SendResetMfaNotificationAsync(new MfaResetRequest { UserName = "tom@yopmail.com" });
        mockSecurityCacheService.Verify(a => a.SetValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()));
        mockCcsSsoEmailService.Verify(a => a.SendResetMfaEmailAsync("tom@yopmail.com", It.IsAny<string>()));
      }
    }

    public class UpdateUser
    {
      public static IEnumerable<object[]> InvalidData =>
                    new List<object[]>
                    {
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("", "lastName", "fl@mail.com"),
                        "ERROR_FIRSTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto(null, "lastName", "fl@mail.com"),
                        "ERROR_FIRSTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "", "fl@mail.com"),
                        "ERROR_LASTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", null, "fl@mail.com"),
                        "ERROR_LASTNAME_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "lastName", ""),
                        "ERROR_EMAIL_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "lastName", null),
                        "ERROR_EMAIL_REQUIRED"
                    },
                    new object[]
                    {
                        DtoHelper.GetUserInfoDto("fisrtName", "lastName", "mail"),
                        "ERROR_EMAIL_FORMAT"
                    },
                    };

      [Theory]
      [MemberData(nameof(InvalidData))]
      public async Task ThrowsException_WhenPassInvalidData(UserInfo userInfo, string expectedError)
      {
        var mockIdentityProviderService = new Mock<IIdentityProviderService>();
        mockIdentityProviderService.Setup(m => m.CreateUserAsync(It.IsAny<UserInfo>()));

        var service = GetUserManagerService(mockIdentityProviderService);
        var ex = await Assert.ThrowsAsync<CcsSsoException>(async () => await service.UpdateUserAsync(userInfo));
        Assert.Equal(expectedError, ex.Message);
      }
    }

    public static UserManagerService GetUserManagerService(Mock<IIdentityProviderService> mockIdentityProviderService = null,
      Mock<ISecurityCacheService> mockSecurityCacheService = null,
      ApplicationConfigurationInfo applicationConfigurationInfo = null, Mock<ICcsSsoEmailService> mockCcsSsoEmailService = null)
    {
      if (mockIdentityProviderService == null)
      {
        mockIdentityProviderService = new Mock<IIdentityProviderService>();
      }

      if (mockSecurityCacheService == null)
      {
        mockSecurityCacheService = new Mock<ISecurityCacheService>();
      }

      if (mockCcsSsoEmailService == null)
      {
        mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
      }

      if (applicationConfigurationInfo == null)
      {
        applicationConfigurationInfo = new ApplicationConfigurationInfo();
      }

      var service = new UserManagerService(mockIdentityProviderService.Object, mockSecurityCacheService.Object, applicationConfigurationInfo, mockCcsSsoEmailService.Object, new RequestContext());
      return service;
    }
  }
}
