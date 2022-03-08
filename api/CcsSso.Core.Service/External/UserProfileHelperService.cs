using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Helpers;

namespace CcsSso.Core.Service.External
{
  public class UserProfileHelperService : IUserProfileHelperService
  {
    public UserProfileHelperService()
    {

    }

    public void ValidateUserName(string userName)
    {
      if (IsInvalidUserName(userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }
      if (userName.Length > Constants.EmailMaxCharaters)
      {
        throw new CcsSsoException(ErrorConstant.ErrorUserIdTooLong);
      }
    }

    public bool IsInvalidUserName(string userName)
    {
      return (string.IsNullOrWhiteSpace(userName) || !UtilityHelper.IsEmailValid(userName));
    }
  }
}
