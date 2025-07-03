using Amazon.S3;
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.DelegationJobScheduler.Model;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CcsSso.Core.DelegationJobScheduler.Services
{
  public class DelegationExpiryNotificationService : IDelegationExpiryNotificationService
  {
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IDelegationExpiryNotificationService> _logger;
    private readonly DelegationAppSettings _appSettings;
    private readonly IEmailProviderService _emaillProviderService;
    private readonly IWrapperUserService _wrapperUserService;
    
    public DelegationExpiryNotificationService(IServiceScopeFactory factory,
      IDateTimeService dateTimeService,
      DelegationAppSettings appSettings,
      ILogger<IDelegationExpiryNotificationService> logger,
      IEmailProviderService emaillProviderService, IWrapperUserService  wrapperUserService )
    {
      _dateTimeService = dateTimeService;
      _appSettings = appSettings;
      _emaillProviderService = emaillProviderService;
      _wrapperUserService = wrapperUserService;
      _logger = logger;
    }
    public async Task PerformNotificationExpiryJobAsync()
    {
      var usersWithinExpiredNotice = await GetUsersWithinExpiredNotice();
      await SendEmailToUsers(usersWithinExpiredNotice);
    }

    private async Task SendEmailToUsers(List<DelegationUserDto> usersWithinExpiredNotice)
    {
      try
      {
        List<Task> emailTaskList = new List<Task>();

        foreach (var item in usersWithinExpiredNotice)
        {

          var userEmailInfo = getDelegatedUserEmailInfo(item.UserName, item.UserName);
          emailTaskList.Add(_emaillProviderService.SendEmailAsync(userEmailInfo));

          _logger.LogInformation($"1. Delegated users EmailId {item.UserName}");

          List<string>? orgAdmins = await _wrapperUserService.GetOrgAdminAsync(item.CiiOrganisationId);

          foreach (var orgAdmin in orgAdmins)
          {
            _logger.LogInformation($"2. Delegated user {item.UserName}'s admin email address {orgAdmin}");

            var adminEmailInfo = getDelegatedAdminEmailInfo(item.UserName, orgAdmin);
            emailTaskList.Add(_emaillProviderService.SendEmailAsync(adminEmailInfo));
          }
        }

        if (!usersWithinExpiredNotice.Any())
        {
          _logger.LogInformation($"1. No users are near to the notification period");
          return;
        }

        _logger.LogInformation($"3. Before sending all the emails");
        await Task.WhenAll(emailTaskList);
        _logger.LogInformation($"4. All the emails has been sent ");

        _logger.LogInformation($"5. Create DelegationEmailNotificationLog logging entries");

				foreach (var user in usersWithinExpiredNotice)
        {
					var delegationAuditEvent = new DelegationEmailNotificationLogInfo
					{
						UserName = user.UserName,
						CiiOrganisationId = user.CiiOrganisationId,
            DelegationEndDate = user.DelegationEndDate
					};

					await _wrapperUserService.CreateDelegationEmailNotificationLog(delegationAuditEvent);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"5. Error while sending email. Error-{ex.Message}");
      }
    }

    private EmailInfo getDelegatedUserEmailInfo(string emailId, string toEmailId)
    {
      var emailTempalteId = _appSettings.EmailSettings.DelegationExpiryNotificationToUserTemplateId;
      var conclaveLoginUrl = _appSettings.ConclaveLoginUrl;

      var data = new Dictionary<string, dynamic>
      {
        { "user-email-id", emailId },
        {"link", conclaveLoginUrl + "/contact-admin" }
      };


      var emailInfo = new EmailInfo
      {
        To = toEmailId,
        TemplateId = emailTempalteId,
        BodyContent = data
      };

      return emailInfo;
    }

    private EmailInfo getDelegatedAdminEmailInfo(string emailId, string toEmailId)
    {
      var emailTempalteId = _appSettings.EmailSettings.DelegationExpiryNotificationToAdminTemplateId;
      var conclaveLoginUrl = _appSettings.ConclaveLoginUrl;

      var data = new Dictionary<string, dynamic>
      {
        { "user-email-id", emailId },
        {"link", conclaveLoginUrl + "/delegated-access" }
      };


      var emailInfo = new EmailInfo
      {
        To = toEmailId,
        TemplateId = emailTempalteId,
        BodyContent = data
      };

      return emailInfo;
    }

		private async Task<List<DelegationUserDto>> GetUsersWithinExpiredNotice()
		{
			var usersNeedNotificationEmail = new List<DelegationUserDto>();
			try
			{
				int duration = _appSettings.DelegationExpiryNotificationJobSettings.ExpiryNoticeInMinutes;

				var untilDate = _dateTimeService.GetUTCNow().AddMinutes(duration).AddDays(-1); // including today. so removing one day from the configured 7 days

        usersNeedNotificationEmail = await _wrapperUserService.GetUsersWithinExpiredNoticeAsync(untilDate.ToString("MM-dd-yyyy")); 
			}
			catch (Exception ex)
			{
				_logger.LogError($"*****Error while getting delegation Expiry Notice users, exception message =  {ex.Message}");
			}
			return usersNeedNotificationEmail;
		}
	}
}
