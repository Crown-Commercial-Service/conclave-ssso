using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CcsSso.Security.Domain.Dtos
{
  public class ChangePasswordDto
  {
    public string UserName { get; set; }

    public string NewPassword { get; set; }

    public string OldPassword { get; set; }
  }

  public class PasswordChallengeDto
  {
    public string UserName { get; set; }

    public string SessionId { get; set; }

    public string NewPassword { get; set; }
  }

  public class ResetPasswordDto
  {
    public string UserName { get; set; }

    public string VerificationCode { get; set; }

    public string NewPassword { get; set; }

  }

  public class ChangePasswordInitiateRequest
  {
    public string UserName { get; set; }

    public bool ForceLogout { get; set; }
  }
}
