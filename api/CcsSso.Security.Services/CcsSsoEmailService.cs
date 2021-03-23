using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  public class CcsSsoEmailService : ICcsSsoEmailService
  {
    private readonly IEmaillProviderService _emaillProviderService;
    private readonly ApplicationConfigurationInfo _appConfigInfo;

    public CcsSsoEmailService(IEmaillProviderService emaillProviderService, ApplicationConfigurationInfo appConfigInfo)
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
        TemplateId = _appConfigInfo.EmailConfigurationInfo.UserActivationEmailTemplateId,
        BodyContent = data
      };
      await _emaillProviderService.SendEmailAsync(emailInfo);
    }

    public async Task SendResetPasswordAsync(string email, string verificationLink)
    {
      var data = new Dictionary<string, dynamic>();
      data.Add("link", verificationLink);
      data.Add("emailid", email);
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailConfigurationInfo.ResetPasswordEmailTemplateId,
        BodyContent = data
      };
      await _emaillProviderService.SendEmailAsync(emailInfo);
    }
  }
}
