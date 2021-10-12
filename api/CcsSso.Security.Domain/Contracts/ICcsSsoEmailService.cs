using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface ICcsSsoEmailService
  {
    Task SendUserActivationLinkAsync(string email, string verificationLink);

    Task SendResetPasswordAsync(string email, string verificationLink);

    Task SendResetMfaEmailAsync(string email, string link);

    Task SendChangePasswordNotificationAsync(string email);
  }
}
