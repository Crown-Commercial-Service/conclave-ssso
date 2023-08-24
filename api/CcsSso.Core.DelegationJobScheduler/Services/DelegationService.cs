using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CcsSso.Core.Domain.Contracts.Wrapper;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace CcsSso.Core.DelegationJobScheduler.Services
{
  public class DelegationService : IDelegationService
  {
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IDelegationService> _logger;
		private readonly IWrapperUserService _wrapperUserService;

		public DelegationService(IDateTimeService dateTimeService, ILogger<IDelegationService> logger, IWrapperUserService wrapperUserService)
    {
      _dateTimeService = dateTimeService;
      _wrapperUserService = wrapperUserService;
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

      foreach (var expiredUser in linkExpiredUsers)
      {
        var delegationAuditEventLog = CreateAuditLog(expiredUser, DelegationAuditEventType.ActivationLinkExpiry);
				await _wrapperUserService.CreateDelegationAuditEvent(delegationAuditEventLog);
      }
     
    }
		private async Task<List<DelegationUserDto>> GetDelegationLinkExpiredUsers()
		{
			var usersWithExpiredLinkNoExpiredLog = new List<DelegationUserDto>();
			try
      {
         usersWithExpiredLinkNoExpiredLog = await _wrapperUserService.GetDelegationLinkExpiredUsersAsync();	
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

			foreach (var delegationEndDatePassedUser in usersWithDelegationEndDatePassed)
			{
				var delegationAuditEventLog = CreateAuditLog(delegationEndDatePassedUser, DelegationAuditEventType.ExpiryOfDelegationAccess);
				await _wrapperUserService.CreateDelegationAuditEvent(delegationAuditEventLog);
				//Delete delegated users
				await _wrapperUserService.DeleteDelegatedUser(delegationEndDatePassedUser.UserName, delegationEndDatePassedUser.CiiOrganisationId);
			}
		}

		private async Task<List<DelegationUserDto>> GetDelegationTerminatedUsers()
		{
			var usersWithDelegationEndDatePassed = new List<DelegationUserDto>();
			try
			{
				usersWithDelegationEndDatePassed = await _wrapperUserService.GetDelegationTerminatedUsersAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError($"*****Error while getting delegation end date passed users, exception message =  {ex.Message}");
			}
			return usersWithDelegationEndDatePassed;
		}

		#endregion

		#region common methods

		private DelegationAuditEventRequestInfo CreateAuditLog(DelegationUserDto linkExpiredUsers, DelegationAuditEventType eventType)
    {
      Guid groupId = Guid.NewGuid();

        var delegationAuditEvent = new DelegationAuditEventRequestInfo
				{
          GroupId = groupId,
          UserName = linkExpiredUsers.UserName,
					CiiOrganisationId = linkExpiredUsers.CiiOrganisationId,
          EventType = eventType.ToString(),
          ActionedOnUtc = _dateTimeService.GetUTCNow(),
          ActionedBy = DelegationAuditActionBy.Job.ToString()
        };

        if (eventType == DelegationAuditEventType.ActivationLinkExpiry)
        {
          _logger.LogInformation($"Delegation link expired for user: {linkExpiredUsers.UserName}, delegation link expired time:{linkExpiredUsers.DelegationLinkExpiryOnUtc}");
        }
        else
        {
          _logger.LogInformation($"Delegation end date passed for user: {linkExpiredUsers.UserName}, delegation start date:{linkExpiredUsers.DelegationStartDate}, end date: {linkExpiredUsers.DelegationEndDate}");
        }

      return delegationAuditEvent;
    }
    #endregion

  }
}
