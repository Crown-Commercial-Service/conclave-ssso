using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  public class CcsSsoEmailService : ICcsSsoEmailService
  {
    private readonly IEmailProviderService _emaillProviderService;
    private readonly ApplicationConfigurationInfo _appConfigInfo;

    public CcsSsoEmailService(IEmailProviderService emaillProviderService, ApplicationConfigurationInfo appConfigInfo)
    {
      _emaillProviderService = emaillProviderService;
      _appConfigInfo = appConfigInfo;
    }

    public async Task SendUserActivationLinkAsync(string email, string verificationLink)
    {
      var data = new Dictionary<string, dynamic>();
      data.Add("link", verificationLink);
      data.Add("emailid", email);
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.CcsEmailConfigurationInfo.UserActivationEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendResetPasswordAsync(string email, string verificationLink)
    {
      var data = new Dictionary<string, dynamic>();
      data.Add("link", verificationLink);
      data.Add("emailid", email);
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.CcsEmailConfigurationInfo.ResetPasswordEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendResetMfaEmailAsync(string email, string link)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "mfaresetlink", link },
        { "emailaddress", email }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.CcsEmailConfigurationInfo.MfaResetEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendChangePasswordNotificationAsync(string email)
    {
      var data = new Dictionary<string, dynamic>();
      data.Add("emailid", email);
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.CcsEmailConfigurationInfo.ChangePasswordNotificationTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    private async Task SendEmailAsync(EmailInfo emailInfo)
    {
      try
      {
        if (_appConfigInfo.CcsEmailConfigurationInfo.SendNotificationsEnabled)
        {
          await _emaillProviderService.SendEmailAsync(emailInfo);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("ERROR_SENDING_EMAIL_NOTIFICATION");
        Console.WriteLine(JsonConvert.SerializeObject(ex));
        throw new CcsSsoException("ERROR_SENDING_EMAIL_NOTIFICATION");
      }
    }
  }
}
