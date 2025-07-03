using CcsSso.Core.PPONScheduler.Model;
using CcsSso.Core.PPONScheduler.Service;
using CcsSso.Core.PPONScheduler.Service.Contracts;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;

namespace CcsSso.Core.PPONScheduler.Jobs
{
  public class PPONJob : BackgroundService
  {
    private readonly PPONAppSettings _appSettings;
    private readonly IPPONService _pPONService;
    private readonly ILogger<PPONJob> _logger;
    private DateTime startDate;
    private DateTime endDate;


    public PPONJob(ILogger<PPONJob> logger, PPONAppSettings appSettings, 
      IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _pPONService = factory.CreateScope().ServiceProvider.GetRequiredService<IPPONService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.ScheduleInMinutes * 60000;

        var oneTimeValidationSwitch = _appSettings.OneTimeJobSettings.Switch;

        _logger.LogInformation("");
        _logger.LogInformation("PPON Scheduled job started at: {time}", DateTimeOffset.Now);

        if (!oneTimeValidationSwitch)
        {
          await _pPONService.PerformJob(oneTimeValidationSwitch, startDate, endDate);
        }

        _logger.LogInformation("PPON Scheduled job Finsied at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");

        await Task.Delay(interval, stoppingToken);
      }
    }     
  }
}
