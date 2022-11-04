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
      await SendEmailAsync(emailInfo);
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
      await SendEmailAsync(emailInfo);
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
