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
      if (!UtilityHelper.IsEmailFormatValid(userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidUserId);
      }
      if (!UtilityHelper.IsEmailLengthValid(userName))
      {
        throw new CcsSsoException(ErrorConstant.ErrorUserIdTooLong);
      }
    }
  }
}
