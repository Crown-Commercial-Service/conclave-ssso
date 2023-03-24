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
    private readonly ICryptographyService _cryptographyService;

    public CcsSsoEmailService(IEmailProviderService emaillProviderService, ApplicationConfigurationInfo appConfigInfo, ICryptographyService cryptographyService)
    {
      _emaillProviderService = emaillProviderService;
      _appConfigInfo = appConfigInfo;
      _cryptographyService = cryptographyService;
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
      string activationInfo = "first=" + orgJoinNotificationInfo.FirstName + "&last=" + orgJoinNotificationInfo.LastName + "&email=" + orgJoinNotificationInfo.Email + 
                              "&org=" + orgJoinNotificationInfo.CiiOrganisationId + "&exp=" + DateTime.UtcNow.AddMinutes(_appConfigInfo.NewUserJoinRequest.LinkExpirationInMinutes);
      var encryptedInfo = _cryptographyService.EncryptString(activationInfo, _appConfigInfo.TokenEncryptionKey);

      var data = new Dictionary<string, dynamic>
      {
        { "firstname", orgJoinNotificationInfo.FirstName },
        { "lastname", orgJoinNotificationInfo.LastName },
        { "email", orgJoinNotificationInfo.Email },
        { "conclaveloginlink", _appConfigInfo.ConclaveSettings.BaseUrl + _appConfigInfo.ConclaveSettings.VerifyUserDetailsRoute + 
        "?details=" + encryptedInfo}
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

    public async Task SendUserRoleApprovalEmailAsync(string email, string userName, string orgName, string serviceName, string encryptedCode)
    {
      var data = new Dictionary<string, dynamic>
                      {
                        { "email", userName},
                        { "orgName", orgName},
                        { "serviceName", serviceName},
                        { "link", _appConfigInfo.ConclaveLoginUrl + "/manage-users/role" + $"?token={encryptedCode}" }
                      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.UserRoleApproval.UserRoleApprovalEmailTemplateId,
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
        TemplateId = _appConfigInfo.OrgAutoValidationEmailInfo.OrgPendingVerificationEmailTemplateId,
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendOrgBuyerStatusChangeUpdateToAllAdminsAsync(string email)
    {

      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.OrgAutoValidationEmailInfo.OrgBuyerStatusChangeUpdateToAllAdmins
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendOrgApproveRightToBuyStatusToAllAdminsAsync(string email)
    {

      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.OrgAutoValidationEmailInfo.ApproveRightToBuyStatusEmailTemplateId
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendOrgDeclineRightToBuyStatusToAllAdminsAsync(string email)
    {

      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.OrgAutoValidationEmailInfo.DeclineRightToBuyStatusEmailTemplateId
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendOrgRemoveRightToBuyStatusToAllAdminsAsync(string email)
    {

      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.OrgAutoValidationEmailInfo.RemoveRightToBuyStatusEmailTemplateId
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
