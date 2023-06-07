using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.DelegationJobScheduler.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DelegationJobScheduler.Jobs
{
  public class DelegationExpiryNotificationJob:BackgroundService
  {
    private readonly DelegationAppSettings _appSettings;
    private readonly IDelegationExpiryNotificationService _delegationExpiryNotificationService;
    private readonly ILogger<DelegationExpiryNotificationJob> _logger;

    public DelegationExpiryNotificationJob(ILogger<DelegationExpiryNotificationJob> logger, DelegationAppSettings appSettings, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _delegationExpiryNotificationService = factory.CreateScope().ServiceProvider.GetRequiredService<IDelegationExpiryNotificationService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.DelegationExpiryNotificationJobSettings.JobFrequencyInMinutes * 60000;

        _logger.LogInformation("*******************************************************************************************");
        _logger.LogInformation("");
        _logger.LogInformation("Delegation notification expiry job started at: {time}", DateTimeOffset.Now);

        await _delegationExpiryNotificationService.PerformNotificationExpiryJobAsync();

        _logger.LogInformation("Delegation notification expiry finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");
        _logger.LogInformation("*******************************************************************************************");

        await Task.Delay(interval, stoppingToken);
      }
    }
  }
}
