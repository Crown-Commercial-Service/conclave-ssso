using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Exceptions;
using CcsSso.Services.Helpers;

namespace CcsSso.Core.Service.External
{
  public class UserProfileHelperService : IUserProfileHelperService
  {
    public UserProfileHelperService()
    {

    }

    public void ValidateUserName(string userName)
    {
      if (string.IsNullOrWhiteSpace(userName) || !UtilitiesHelper.IsEmailValid(userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }
    }
  }
}
