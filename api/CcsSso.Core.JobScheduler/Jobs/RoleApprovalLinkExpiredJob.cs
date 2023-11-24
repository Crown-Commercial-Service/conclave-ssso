using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Jobs
{
  public class RoleApprovalLinkExpiredJob : BackgroundService
  {
    private readonly AppSettings _appSettings;
    private readonly IRoleApprovalLinkExpiredService _roleDeleteExpiredNotificationService;
    private readonly ILogger<RoleApprovalLinkExpiredJob> _logger;
    private bool enable;
    private IWrapperUserService _wrapperUserService;
    public RoleApprovalLinkExpiredJob(ILogger<RoleApprovalLinkExpiredJob> logger, IServiceScopeFactory factory,
      AppSettings appSettings, IWrapperUserService wrapperUserService)
    {
      _appSettings = appSettings;
      _roleDeleteExpiredNotificationService = factory.CreateScope().ServiceProvider.GetRequiredService<IRoleApprovalLinkExpiredService>();
      _logger = logger;
      enable = false;
      _wrapperUserService = wrapperUserService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        enable = _appSettings.ActiveJobStatus.RoleDeleteExpiredNotificationJob;
        int interval = _appSettings.ScheduleJobSettings.RoleExpiredNotificationDeleteFrequencyInMinutes * 60000;

        if (!enable)
        {
          _logger.LogInformation($"****** Delete expired notification role job is disabled. Skipping this iteration");
          await Task.Delay(interval, stoppingToken);
          continue;
        }

        _logger.LogInformation($" ****************Delete expired role notification job started ***********");
        await PerformJobAsync();
        _logger.LogInformation($"******************Delete expired role notification job  Finished  ***********");

        await Task.Delay(interval, stoppingToken);
      }
    }

    private async Task PerformJobAsync()
    {
      try
      {
        UserAccessRolePendingFilterCriteria criteria = new UserAccessRolePendingFilterCriteria() { Status = UserPendingRoleStaus.Pending };
        var userPendingRole = await _wrapperUserService.GetUserAccessRolePendingDetails(criteria);

        _logger.LogInformation($"****** Pending role approval request: {userPendingRole.UserAccessRolePendingDetailsInfo.Count()}");

        if (userPendingRole.UserAccessRolePendingDetailsInfo.Any())
        {
          await _roleDeleteExpiredNotificationService.PerformJobAsync(userPendingRole.UserAccessRolePendingDetailsInfo);
        }
        else
        {
          _logger.LogInformation($"****** No Pending role approval request found.");
        }

      }
      catch (CcsSsoException ex) 
      {
        _logger.LogError($"{ex.Message}");
      }
      catch (Exception e)
      {
        _logger.LogError($"****** Error while deleting the expired user role notification: {e.Message}");
      }
    }
  }
}