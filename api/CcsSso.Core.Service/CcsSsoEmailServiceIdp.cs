using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Newtonsoft.Json;
using System;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Domain.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public partial class CcsSsoEmailService : ICcsSsoEmailService
  {

    public async Task SendUserUpdateEmailOnlyUserIdPwdAsync(string email, string activationlink)
    {
      var data = new Dictionary<string, dynamic>
      {
          { "link", activationlink }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserUpdateEmailOnlyUserIdPwdTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendUserUpdateEmailOnlyFederatedIdpAsync(string email, string idpName)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "sigininproviders", idpName },
        { "federatedlogin", _appConfigInfo.ConclaveLoginUrl }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserUpdateEmailOnlyFederatedIdpTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendUserUpdateEmailBothIdpAsync(string email, string idpName, string activationlink)
    {
      var data = new Dictionary<string, dynamic>
      {
         { "sigininproviders", idpName },
        { "federatedlogin", _appConfigInfo.ConclaveLoginUrl },
        { "link", activationlink }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserUpdateEmailBothIdpTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    // #Auto validation
    public async Task SendUserConfirmEmailOnlyUserIdPwdAsync(string email, string activationlink, string ccsMsg)
    {
      await SendUserConfirmEmailOnlyUserIdPwdAsync(email, activationlink, ccsMsg, true);
    }
    public async Task SendUserConfirmEmailOnlyUserIdPwdAsync(string email, string activationlink, string ccsMsg, bool isUserInAuth0)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "link", activationlink },
        { "CCSMsg", ccsMsg}
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserConfirmEmailOnlyUserIdPwdTemplateId,
        BodyContent = data
      };

      if (!_appConfigInfo.EmailInfo.SendNotificationsEnabled)
      {
        return;
      }
      if (_appConfigInfo.NotificationApiSettings.Enable)
      {
        try
        {
          EmailResquestInfo emailResquestInfo = new EmailResquestInfo { EmailInfo = emailInfo, IsUserInAuth0 = isUserInAuth0 };
          var isEmailSuccess = await _notificationApiService.PostAsync<bool>($"notification/senduserconfirmemail", emailResquestInfo, "ERROR_SENDING_EMAIL_NOTIFICATION");
          if (!isEmailSuccess)
          {
            Console.WriteLine("RateLimitCheck: Notification api returns false while sending the email with activation link");
            Console.WriteLine("ERROR_SENDING_EMAIL_NOTIFICATION");
            throw new CcsSsoException("ERROR_SENDING_EMAIL_NOTIFICATION");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine("RateLimitCheck: Exception while calling Notification api to send email with activation link");

          Console.WriteLine("ERROR_SENDING_EMAIL_NOTIFICATION");
          Console.WriteLine(JsonConvert.SerializeObject(ex));
          throw new CcsSsoException("ERROR_SENDING_EMAIL_NOTIFICATION");
        }
      }
      else
      {
        await SendEmailAsync(emailInfo);
      }
    }
    public async Task SendUserConfirmEmailOnlyFederatedIdpAsync(string email, string idpName)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "sigininproviders", idpName },
        { "federatedlogin", _appConfigInfo.ConclaveLoginUrl }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserConfirmEmailOnlyFederatedIdpTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendUserConfirmEmailBothIdpAsync(string email, string idpName, string activationlink)
    {
      await SendUserConfirmEmailBothIdpAsync(email, idpName, activationlink, true);
    }

    public async Task SendUserConfirmEmailBothIdpAsync(string email, string idpName, string activationlink, bool isUserInAuth0)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "sigininproviders", idpName },
        { "federatedlogin", _appConfigInfo.ConclaveLoginUrl },
        { "link", activationlink }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserConfirmEmailBothIdpTemplateId,
        BodyContent = data
      };

      if (!_appConfigInfo.EmailInfo.SendNotificationsEnabled)
      {
        return;
      }
      if (_appConfigInfo.NotificationApiSettings.Enable)
      {
        try
        {
          EmailResquestInfo emailResquestInfo = new EmailResquestInfo { EmailInfo = emailInfo, IsUserInAuth0 = isUserInAuth0 };
          var isEmailSuccess = await _notificationApiService.PostAsync<bool>($"notification/senduserconfirmemail", emailResquestInfo, "ERROR_SENDING_EMAIL_NOTIFICATION");
          if (!isEmailSuccess)
          {
            Console.WriteLine("ERROR_SENDING_EMAIL_NOTIFICATION");
            throw new CcsSsoException("ERROR_SENDING_EMAIL_NOTIFICATION");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine("ERROR_SENDING_EMAIL_NOTIFICATION");
          Console.WriteLine(JsonConvert.SerializeObject(ex));
          throw new CcsSsoException("ERROR_SENDING_EMAIL_NOTIFICATION");
        }
      }
      else
      {
        await SendEmailAsync(emailInfo);
      }
    }

    public async Task SendUserRegistrationEmailUserIdPwdAsync(string email,  string activationlink)
    {
      var data = new Dictionary<string, dynamic>
      {
        { "link", activationlink }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserRegistrationEmailUserIdPwdTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }
    


  }
}
