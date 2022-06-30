using CcsSso.Core.ReportingScheduler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ReportingScheduler.Jobs
{
  public class ContactReportingJob : BackgroundService
  {
    private readonly ILogger<ContactReportingJob> _logger;
    private readonly AppSettings _appSettings;

    public ContactReportingJob(ILogger<ContactReportingJob> logger, AppSettings appSettings)
    {
      _logger = logger;
      _appSettings = appSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        _logger.LogInformation("User Reporting Job  running at: {time}", DateTimeOffset.Now);
        await Task.Delay(1000, stoppingToken);
      }
    }
  }
}
