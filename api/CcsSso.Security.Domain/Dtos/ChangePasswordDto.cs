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
    [Required]
    public string UserName { get; set; }

    [Required]
    public string SessionId { get; set; }

    [Required]
    public string NewPassword { get; set; }
  }

  public class ResetPasswordDto
  {
    [Required]
    public string UserName { get; set; }

    [Required]
    public string VerificationCode { get; set; }

    [Required]
    public string NewPassword { get; set; }

  }
}
