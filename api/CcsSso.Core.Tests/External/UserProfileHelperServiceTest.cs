using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class UserProfileHelperServiceTest
  {

    public class ValidateUserName
    {

      [Theory]
      [InlineData("correctemail@mail.com")]
      public async Task NotThrowsException_WhenValidUserName(string userName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          var userHelperService = UserProfileHelperService(dataContext);

          var exception = Record.Exception(() => userHelperService.ValidateUserName(userName));
          Assert.Null(exception);
        });
      }

      [Theory]
      [InlineData("wrongemail.com")]
      [InlineData("wrongemail")]
      [InlineData(null)]
      [InlineData("")]
      [InlineData(" ")]
      public async Task ThrowsException_WhenInvalidUserName(string userName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          var userHelperService = UserProfileHelperService(dataContext);

          var ex = Assert.Throws<CcsSsoException>(() => userHelperService.ValidateUserName(userName));
          Assert.Equal(ErrorConstant.ErrorInvalidUserId, ex.Message);
        });
      }
    }

    public static UserProfileHelperService UserProfileHelperService(IDataContext dataContext)
    {
      var service = new UserProfileHelperService();
      return service;
    }
  }
}
