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

    public static UserManagerService GetUserManagerService(Mock<IIdentityProviderService> mockIdentityProviderService = null)
    {
      if (mockIdentityProviderService == null)
      {
        mockIdentityProviderService = new Mock<IIdentityProviderService>();
      }
      var service = new UserManagerService(mockIdentityProviderService.Object);
      return service;
    }
  }
}
