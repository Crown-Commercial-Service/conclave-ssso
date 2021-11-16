using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
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
        var emailInfo = new EmailInfo()
        {
          To = toEmail,
          TemplateId = _emailConfigurationInfo.UnverifiedUserDeletionNotificationTemplateId,
          BodyContent = data
        };

        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }
      await Task.WhenAll(emailTaskList);
    }
  }
}
