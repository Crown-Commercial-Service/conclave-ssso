using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public partial class CcsSsoEmailService : ICcsSsoEmailService
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
      await SendEmailAsync(emailInfo);
    }

    public async Task SendNominateEmailAsync(string email, string link)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "OrgRegistersationlink", link },
        { "emailaddress", email }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.NominateEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
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
        await SendEmailAsync(emailInfo);
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
        await SendEmailAsync(emailInfo);
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
        await SendEmailAsync(emailInfo);
      }
    }

    public async Task SendOrgJoinRequestEmailAsync(OrgJoinNotificationInfo orgJoinNotificationInfo)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "firstname", orgJoinNotificationInfo.FirstName },
        { "lastname", orgJoinNotificationInfo.LastName },
        { "email", orgJoinNotificationInfo.Email },
        { "conclaveloginlink", _appConfigInfo.ConclaveSettings.BaseUrl }
      };
      var emailInfo = new EmailInfo()
      {
        To = orgJoinNotificationInfo.ToEmail,
        TemplateId = _appConfigInfo.EmailInfo.OrganisationJoinRequestTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendUserPermissionUpdateEmailAsync(string email)
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
      await SendEmailAsync(emailInfo);
    }
    // #Delegated
    public async Task SendUserDelegatedAccessEmailAsync(string email, string orgName, string encryptedCode)
    {
      var data = new Dictionary<string, dynamic>
                      {
                        { "orgName", orgName},
                        { "link", _appConfigInfo.ConclaveLoginUrl + "/delegated-user-activation" + $"?activationcode={encryptedCode}" }
                      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserDelegatedAccessEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    // #Auto validation
    public async Task SendOrgPendingVerificationEmailToCCSAdminAsync(string email, string orgName)
    {
      var data = new Dictionary<string, dynamic>
                      {
                        { "orgName", orgName},
                        { "link", _appConfigInfo.ConclaveLoginUrl + "/manage-buyer-both" }
                      };

      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.OrgPendingVerificationEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    private async Task SendEmailAsync(EmailInfo emailInfo)
    {
      try
      {
        if (_appConfigInfo.EmailInfo.SendNotificationsEnabled)
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
