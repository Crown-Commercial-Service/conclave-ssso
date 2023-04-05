using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Jobs
{
  public class RoleApprovalLinkExpiredJob : BackgroundService
  {
    private readonly IDataContext _dataContext;
    private readonly AppSettings _appSettings;
    private readonly IRoleApprovalLinkExpiredService _roleDeleteExpiredNotificationService;

    private readonly ILogger<RoleApprovalLinkExpiredJob> _logger;
    private bool enable;

    public RoleApprovalLinkExpiredJob(ILogger<RoleApprovalLinkExpiredJob> logger, IServiceScopeFactory factory,
      AppSettings appSettings)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();

      _appSettings = appSettings;
      _roleDeleteExpiredNotificationService = factory.CreateScope().ServiceProvider.GetRequiredService<IRoleApprovalLinkExpiredService>(); ;
      _logger = logger;
      enable = false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        enable = _appSettings.ActiveJobStatus.RoleDeleteExpiredNotificationJob;
        int interval = _appSettings.ScheduleJobSettings.RoleExpiredNotificationDeleteFrequencyInMinutes * 60000;

        if (!enable)
        {
          _logger.LogInformation($"Delete expired notification role job is disabled. Skipping this iteration");
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
        var userPendingRole = await GetPendingRoleApproval();

        _logger.LogInformation($"Pending Role approval request: {userPendingRole.Count()}");

        await _roleDeleteExpiredNotificationService.PerformJobAsync(userPendingRole);


      }
      catch (Exception e)
      {
        _logger.LogError($"Error while deleting the expired user role notification: {e.Message}");
      }

    }

    public async Task<List<UserAccessRolePending>> GetPendingRoleApproval()
    {
      var userAccessRolePendingAllList = await _dataContext.UserAccessRolePending
       .Include(u => u.OrganisationEligibleRole).ThenInclude(or => or.CcsAccessRole)
       .Where(u => !u.IsDeleted && u.Status == (int)UserPendingRoleStaus.Pending)
       .ToListAsync();

      return userAccessRolePendingAllList;
    }
  }
}
