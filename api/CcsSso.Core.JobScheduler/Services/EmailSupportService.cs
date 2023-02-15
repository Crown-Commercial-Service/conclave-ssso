using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Services
{
  public class EmailSupportService : IEmailSupportService
  {
    private readonly IEmailProviderService _emaillProviderService;
    private readonly EmailConfigurationInfo _emailConfigurationInfo;

    public EmailSupportService(IEmailProviderService emaillProviderService, EmailConfigurationInfo emailConfigurationInfo)
    {
      _emaillProviderService = emaillProviderService;
      _emailConfigurationInfo = emailConfigurationInfo;
    }

    public async Task SendUnVerifiedUserDeletionEmailToAdminAsync(string name, string email, List<string> toEmails)
    {
      var data = new Dictionary<string, dynamic>
        {
          { "fullname", name },
          { "emailaddress", email }
        };

      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in toEmails)
      {
        var emailInfo = GetEmailInfo(toEmail, _emailConfigurationInfo.UnverifiedUserDeletionNotificationTemplateId, data);

        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }
      await Task.WhenAll(emailTaskList);
    }

    public async Task SendBulUploadResultEmailAsync(string toEmail, string resultStatus, string reportUrl)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "resultStatus", resultStatus },
        { "reportUrl",  reportUrl}
      };

      var emailInfo = GetEmailInfo(toEmail, _emailConfigurationInfo.BulkUploadReportTemplateId, data);

      await _emaillProviderService.SendEmailAsync(emailInfo);
    }

    public async Task SendRoleRejectedEmailAsync(string email, string userName, string serviceName)
    {
      var data = new Dictionary<string, dynamic>
      {
          { "email", userName},
          { "serviceName", serviceName}
      };

      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _emailConfigurationInfo.UserRoleExpiredEmailTemplateId,
        BodyContent = data
      };

      await _emaillProviderService.SendEmailAsync(emailInfo);
    }


    private EmailInfo GetEmailInfo(string toEmail, string templateId, Dictionary<string, dynamic> data)
    {
      var emailInfo = new EmailInfo
      {
        To = toEmail,
        TemplateId = templateId,
        BodyContent = data
      };

      return emailInfo;
    }
  }
}
