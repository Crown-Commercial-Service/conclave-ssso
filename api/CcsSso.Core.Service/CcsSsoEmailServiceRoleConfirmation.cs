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

    public async Task SendRoleApprovedEmailAsync(string email, string link)
    {
      var data = new Dictionary<string, dynamic>
      {
          { "dashboardlink", link }
      };
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.UserRoleApproval.UserRoleApprovedEmailTemplateId, 
        BodyContent = data
      };
      await SendEmailAsync(emailInfo);
    }

    public async Task SendRoleRejectedEmailAsync(string email)
    {
      var emailInfo = new EmailInfo()
      {
        To = email,
        TemplateId = _appConfigInfo.EmailInfo.UserUpdateEmailOnlyUserIdPwdTemplateId 
      };

      await SendEmailAsync(emailInfo);
    }
  }
}
