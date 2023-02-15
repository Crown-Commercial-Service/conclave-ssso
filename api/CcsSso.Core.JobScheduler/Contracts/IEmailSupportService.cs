using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IEmailSupportService
  {
    Task SendUnVerifiedUserDeletionEmailToAdminAsync(string name, string email, List<string> toEmails);

    Task SendBulUploadResultEmailAsync(string toEmail, string resultStatus, string reportUrl);

    Task SendRoleRejectedEmailAsync(string email, string userName, string serviceName);
  }
}
