using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Jobs
{
  public class UserArchivalJob : BackgroundService
  {
    private readonly DormancyAppSettings _appSettings;
    private readonly IUserArchivalService _userArchivalService;
    private readonly ILogger<UserArchivalJob> _logger;

    public UserArchivalJob(ILogger<UserArchivalJob> logger, DormancyAppSettings appSettings, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _userArchivalService = factory.CreateScope().ServiceProvider.GetRequiredService<IUserArchivalService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.DormancyJobSettings.ArchivalJobFrequencyInMinutes * 60000;

        _logger.LogInformation("*******************************************************************************************");
        _logger.LogInformation("");
        _logger.LogInformation("User archival job started at: {time}", DateTimeOffset.Now);

        await _userArchivalService.PerformUserArchivalJobAsync();

        _logger.LogInformation("User archival job finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");
        _logger.LogInformation("*******************************************************************************************");

        await Task.Delay(interval, stoppingToken);
      }
    }
  }
}
