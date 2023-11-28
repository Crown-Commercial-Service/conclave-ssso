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
  public class DormancyNotificationJob:BackgroundService
  {
    private readonly DormancyAppSettings _appSettings;
    private readonly IDormancyNotificationService _dormancyNotificationService;
    private readonly ILogger<DormancyNotificationJob> _logger;

    public DormancyNotificationJob(ILogger<DormancyNotificationJob> logger, DormancyAppSettings appSettings, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dormancyNotificationService = factory.CreateScope().ServiceProvider.GetRequiredService<IDormancyNotificationService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.DormancyJobSettings.DormancyNotificationJobFrequencyInMinutes * 60000;

        _logger.LogInformation("*******************************************************************************************");
        _logger.LogInformation("");
        _logger.LogInformation("User Dormant notification job started at: {time}", DateTimeOffset.Now);

        await _dormancyNotificationService.PerformDormancyNotificationJobAsync();

        _logger.LogInformation("User Dormant notification finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");
        _logger.LogInformation("*******************************************************************************************");

        await Task.Delay(interval, stoppingToken);
      }
    }
  }
}
