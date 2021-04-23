using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
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

    public async Task SendUserWelcomeEmailAsync(string email, string idpName)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "emailaddress", email },
        { "idp", idpName },
        { "ConclaveLoginlink", _appConfigInfo.ConclaveLoginUrl }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserWelcomeEmailTemplateId,
        BodyContent = data
      };
      await _emaillProviderService.SendEmailAsync(emailInfo);
    }

    public async Task SendOrgProfileUpdateEmailAsync(string email)
    {
      if (_appConfigInfo.EmailInfo.SendNotificationsEnabled)
      {
        var emailInfo = new EmailInfo()
        {
          To = email,
          TemplateId = _appConfigInfo.EmailInfo.OrgProfileUpdateNotificationTemplateId
        };
        await _emaillProviderService.SendEmailAsync(emailInfo);
      }
    }

    public async Task SendUserProfileUpdateEmailAsync(string email)
    {
      if (_appConfigInfo.EmailInfo.SendNotificationsEnabled)
      {
        var emailInfo = new EmailInfo()
        {
          To = email,
          TemplateId = _appConfigInfo.EmailInfo.UserProfileUpdateNotificationTemplateId
        };
        await _emaillProviderService.SendEmailAsync(emailInfo);
      }
    }

    public async Task SendUserContactUpdateEmailAsync(string email)
    {
      if (_appConfigInfo.EmailInfo.SendNotificationsEnabled)
      {
        var emailInfo = new EmailInfo()
        {
          To = email,
          TemplateId = _appConfigInfo.EmailInfo.UserContactUpdateNotificationTemplateId
        };
        await _emaillProviderService.SendEmailAsync(emailInfo);
      }
    }

    public async Task SendUserPermissionUpdateEmailAsync(string email)
    {
      if (_appConfigInfo.EmailInfo.SendNotificationsEnabled)
      {
        var data = new Dictionary<string, dynamic>
        {
          { "emailid", email }
        };

        var emailInfo = new EmailInfo()
        {
          To = email,
          TemplateId = _appConfigInfo.EmailInfo.UserPermissionUpdateNotificationTemplateId,
          BodyContent = data
        };
        await _emaillProviderService.SendEmailAsync(emailInfo);
      }
    }
  }
}
