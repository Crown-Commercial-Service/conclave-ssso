using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CcsSso.Core.DelegationJobScheduler.Services
{
  public class DelegationService : IDelegationService
  {
    private readonly IDataContext _dataContext;
    private readonly IDelegationAuditEventService _delegationAuditEventService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IDelegationService> _logger;

    public DelegationService(IServiceScopeFactory factory, IDateTimeService dateTimeService, ILogger<IDelegationService> logger, IDelegationAuditEventService delegationAuditEventService)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _delegationAuditEventService = delegationAuditEventService;
      _dateTimeService = dateTimeService;
      _logger = logger;
    }

    #region Link expired job
    public async Task PerformLinkExpireJobAsync()
    {
      var linkExpiredUsers = await GetDelegationLinkExpiredUsers();

      if (!linkExpiredUsers.Any())
      {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation("No user with expired delegated link found.");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        return;
      }

      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      _logger.LogInformation($"Number of users with expired delegated link: {linkExpiredUsers.Count()}");
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

      var delegationAuditEventLogs = CreateAuditLogs(linkExpiredUsers, DelegationAuditEventType.ActivationLinkExpiry);
      await _delegationAuditEventService.CreateDelegationAuditEventsAsync(delegationAuditEventLogs);
    }

    private async Task<List<User>> GetDelegationLinkExpiredUsers()
    {
      var usersWithExpiredLinkNoExpiredLog = new List<User>();
      try
      {
        var usersWithExpiredLink = await _dataContext.User.Where(u => !u.IsDeleted && u.UserType == UserType.Delegation && !u.DelegationAccepted && u.DelegationLinkExpiryOnUtc < _dateTimeService.GetUTCNow()).ToListAsync();
        
        foreach (var user in usersWithExpiredLink) 
        {
          var linkExpiredUsersLastAuditLog = _dataContext.DelegationAuditEvent.OrderByDescending(x => x.Id).FirstOrDefault(u => u.UserId == user.Id);
          if (linkExpiredUsersLastAuditLog?.EventType != DelegationAuditEventType.ActivationLinkExpiry.ToString())
          {
            usersWithExpiredLinkNoExpiredLog.Add(user);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting delegation link expired users, exception message =  {ex.Message}");
      }
      return usersWithExpiredLinkNoExpiredLog;
    }
    #endregion

    #region Delegation termination 
    public async Task PerformDelegationTermissionJobAsync()
    {
      var usersWithDelegationEndDatePassed = await GetDelegationTerminatedUsers();

      if (!usersWithDelegationEndDatePassed.Any())
      {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation("No user with delegation end date passed found.");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        return;
      }

      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      _logger.LogInformation($"Number of users with delegation end date passed {usersWithDelegationEndDatePassed.Count()}");
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

      var delegationAuditEventLogs = CreateAuditLogs(usersWithDelegationEndDatePassed, DelegationAuditEventType.ExpiryOfDelegationAccess);
      await _delegationAuditEventService.CreateDelegationAuditEventsAsync(delegationAuditEventLogs);
      //Delete delegated users
      await DeleteDelegationTerminatedUsers(usersWithDelegationEndDatePassed);
    }

    private async Task<List<User>> GetDelegationTerminatedUsers()
    {
      var usersWithDelegationEndDatePassed = new List<User>();
      try
      {
        usersWithDelegationEndDatePassed = await _dataContext.User.Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation).Include(u => u.UserAccessRoles)
          .Where(u => !u.IsDeleted && u.UserType == UserType.Delegation && u.DelegationEndDate < _dateTimeService.GetUTCNow().Date).ToListAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting delegation end date passed users, exception message =  {ex.Message}");
      }
      return usersWithDelegationEndDatePassed;
    }

    private async Task DeleteDelegationTerminatedUsers(List<User> users) 
    {
      foreach (var user in users)
      {
        user.Party.IsDeleted = true;
        user.Party.Person.IsDeleted = true;
        user.LastUpdatedOnUtc = _dateTimeService.GetUTCNow();
        
        if (user.UserAccessRoles != null)
        {
          user.UserAccessRoles.ForEach((userAccessRole) =>
          {
            userAccessRole.IsDeleted = true;
          });
        }
        user.IsDeleted = true;
      }
      await _dataContext.SaveChangesAsync();
    }
    #endregion

    #region common methods

    private List<DelegationAuditEventInfo> CreateAuditLogs(List<User> linkExpiredUsers, DelegationAuditEventType eventType)
    {
      Guid groupId = Guid.NewGuid();

      List<DelegationAuditEventInfo> delegationAuditEventInfoList = new();

      foreach (var user in linkExpiredUsers)
      {
        var delegationAuditEvent = new DelegationAuditEventInfo
        {
          GroupId = groupId,
          UserId = user.Id,
          EventType = eventType.ToString(),
          ActionedOnUtc = _dateTimeService.GetUTCNow()
        };

        if (eventType == DelegationAuditEventType.ActivationLinkExpiry)
        {
          _logger.LogInformation($"Delegation link expired for user: {user.UserName}, delegation link expired time:{user.DelegationLinkExpiryOnUtc}");
        }
        else
        {
          _logger.LogInformation($"Delegation end date passed for user: {user.UserName}, delegation start date:{user.DelegationStartDate}, end date: {user.DelegationEndDate}");
        }

        delegationAuditEventInfoList.Add(delegationAuditEvent);
      }

      return delegationAuditEventInfoList;
    }
    #endregion

  }
}
