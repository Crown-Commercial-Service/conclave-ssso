using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.DelegationJobScheduler.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CcsSso.Core.DelegationJobScheduler.Jobs
{
  public class LinkExpiryJob : BackgroundService
  {
    private readonly DelegationAppSettings _appSettings;
    private readonly IDelegationService _delegationService;
    private readonly ILogger<LinkExpiryJob> _logger;

    public LinkExpiryJob(ILogger<LinkExpiryJob> logger, DelegationAppSettings appSettings, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _delegationService = factory.CreateScope().ServiceProvider.GetRequiredService<IDelegationService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.DelegationJobSettings.DelegationLinkExpiryJobFrequencyInMinutes * 60000;

        _logger.LogInformation("*******************************************************************************************");
        _logger.LogInformation("");
        _logger.LogInformation("Delegation link expiry job started at: {time}", DateTimeOffset.Now);

        await _delegationService.PerformLinkExpireJobAsync();
        
        _logger.LogInformation("Delegation link expiry finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");
        _logger.LogInformation("*******************************************************************************************");

        await Task.Delay(interval, stoppingToken);
      }
    }
  }
}
