using Amazon.S3;
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.DelegationJobScheduler.Model;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notify.Client;
using Notify.Models;
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
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IDelegationExpiryNotificationService> _logger;
    private readonly DelegationAppSettings _appSettings;
    private readonly IEmailProviderService _emaillProviderService;

    public DelegationExpiryNotificationService(IServiceScopeFactory factory,
      IDateTimeService dateTimeService,
      DelegationAppSettings appSettings,
      ILogger<IDelegationExpiryNotificationService> logger,
      IEmailProviderService emaillProviderService)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dateTimeService = dateTimeService;
      _appSettings = appSettings;
      _emaillProviderService = emaillProviderService;

      _logger = logger;
    }
    public async Task PerformNotificationExpiryJobAsync()
    {
      var usersWithinExpiredNotice = await GetUsersWithinExpiredNotice();
      await SendEmailToUsers(usersWithinExpiredNotice);
    }

    private async Task SendEmailToUsers(List<User> usersWithinExpiredNotice)
    {
      try
      {
        List<Task> emailTaskList = new List<Task>();

        foreach (var item in usersWithinExpiredNotice)
        {

          var userEmailInfo = getDelegatedUserEmailInfo(item.UserName, item.UserName);
          emailTaskList.Add(_emaillProviderService.SendEmailAsync(userEmailInfo));

          _logger.LogInformation($"1. Delegated users EmailId {item.UserName}");

          List<string>? orgAdmins = await GetOrgAdmin(item);

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
        await CreateAuditLogs(usersWithinExpiredNotice);
      }
      catch (Exception ex)
      {
        _logger.LogError($"5. Error while sending email. Error-{ex.Message}");
      }
    }

    private async Task CreateAuditLogs(List<User> usersWithinExpiredNotice)
    {
      List<DelegationEmailNotificationLog> delegationEmailNotificationLogList = new();

      foreach (var user in usersWithinExpiredNotice)
      {
        var delegationAuditEvent = new DelegationEmailNotificationLog
        {

          UserId = user.Id,
          DelegationEndDate = (DateTime)user.DelegationEndDate,
          NotifiedOnUtc = _dateTimeService.GetUTCNow(),
        };

        delegationEmailNotificationLogList.Add(delegationAuditEvent);
      }

      _dataContext.DelegationEmailNotificationLog.AddRange(delegationEmailNotificationLogList);

      await _dataContext.SaveChangesAsync();
    }

    private async Task<List<string>> GetOrgAdmin(User item)
    {
      var organisationId = item.Party.Person.OrganisationId;

      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
       .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId &&
       or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey))?.Id;

      var orgAdmins = await _dataContext.User.Where(u => !u.IsDeleted && u.AccountVerified
       && u.Party.Person.OrganisationId == organisationId
       && (u.UserGroupMemberships.Any(ugm => !ugm.IsDeleted
       && ugm.OrganisationUserGroup.GroupEligibleRoles.Any(ga => !ga.IsDeleted && ga.OrganisationEligibleRoleId == orgAdminAccessRoleId))
       || u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId)))
      .Select(u => u).OrderBy(u => u.Id).ToListAsync();

      return orgAdmins.Select(x => x.UserName).ToList();
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

    private async Task<List<User>> GetUsersWithinExpiredNotice()
    {
      var usersNeedNotificationEmail = new List<User>();
      try
      {
        int duration = _appSettings.DelegationExpiryNotificationJobSettings.ExpiryNoticeInMinutes;

        var untilDate = _dateTimeService.GetUTCNow().AddMinutes(duration).AddDays(-1); // including today. so removing one day from the configured 7 days

        var usersWithinExpiredNotice = await _dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person)
          .Where(u => !u.IsDeleted
          && u.UserType == UserType.Delegation
          && u.DelegationAccepted
          && u.DelegationEndDate.Value.Date == untilDate.Date).ToListAsync();

        foreach (var user in usersWithinExpiredNotice)
        {
          var linkExpiredUsersLastAuditLog = _dataContext.DelegationEmailNotificationLog.OrderByDescending(x => x.Id).FirstOrDefault(u => u.UserId == user.Id && u.DelegationEndDate == user.DelegationEndDate);
          if (linkExpiredUsersLastAuditLog == null)
          {
            usersNeedNotificationEmail.Add(user);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting delegation Expiry Notice users, exception message =  {ex.Message}");
      }
      return usersNeedNotificationEmail;
    }
  }
}
