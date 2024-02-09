using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Jobs
{
  public class UserDeactivationJob:BackgroundService
  {
    private readonly DormancyAppSettings _appSettings;
    private readonly IUserDeactivationService _userDectivationService;
    private readonly ILogger<IUserDeactivationService> _logger;

    public UserDeactivationJob(ILogger<IUserDeactivationService> logger, DormancyAppSettings appSettings, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _userDectivationService = factory.CreateScope().ServiceProvider.GetRequiredService<IUserDeactivationService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.DormancyJobSettings.UserDeactivationJobFrequencyInMinutes * 60000;

        _logger.LogInformation("*******************************************************************************************");
        _logger.LogInformation("");
        _logger.LogInformation("UserDeactivation job started at: {time}", DateTimeOffset.Now);

        await _userDectivationService.PerformUserDeactivationJobAsync();

        _logger.LogInformation("UserDeactivation job finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");
        _logger.LogInformation("*******************************************************************************************");

        await Task.Delay(interval, stoppingToken);
      }
    }
  }
}
