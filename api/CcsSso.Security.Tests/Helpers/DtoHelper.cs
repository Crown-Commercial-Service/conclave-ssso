using CcsSso.Security.Domain.Dtos;

namespace CcsSso.Security.Tests.Helpers
{
  public static class DtoHelper
  {
    public static UserInfo GetUserInfoDto(string firstName, string lastName, string email)
    {
      return new UserInfo
      {
        FirstName = firstName,
        LastName = lastName,
        Email = email
      };
    }

    public static ChangePasswordDto GetChangePasswordDto(string userName, string newPassword, string oldPassword)
    {
      var changePasswordRequest = new ChangePasswordDto()
      {
        UserName = userName,
        NewPassword = newPassword,
        OldPassword = oldPassword
      };
      return changePasswordRequest;
    }

    public static ResetPasswordDto GetResetPasswordDto(string userName, string newPassword, string verificationCode)
    {
      var resetPasswordRequest = new ResetPasswordDto()
      {
        UserName = userName,
        NewPassword = newPassword,
        VerificationCode = verificationCode
      };
      return resetPasswordRequest;
    }

    public static PasswordChallengeDto GetPasswordChallengeDto(string userName, string newPassword, string sessionId)
    {
      var passwordChallengeDto = new PasswordChallengeDto()
      {
        UserName = userName,
        NewPassword = newPassword,
        SessionId = sessionId
      };
      return passwordChallengeDto;
    }
  }
}
