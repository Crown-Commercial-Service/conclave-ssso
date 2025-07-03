using CcsSso.Core.PPONScheduler.Model;
using CcsSso.Core.PPONScheduler.Service.Contracts;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using System.Globalization;

namespace CcsSso.Core.PPONScheduler.Jobs
{
  public class OneTimePPONJob : BackgroundService
  {
    private readonly PPONAppSettings _appSettings;
    private readonly IDataContext _dataContext;
    private readonly IPPONService _pPONService;
    private readonly ILogger<OneTimePPONJob> _logger;
    private bool ranOnce;
    private DateTime startDate;
    private DateTime endDate;


    public OneTimePPONJob(ILogger<OneTimePPONJob> logger, PPONAppSettings appSettings,
      IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      ranOnce = false;
      _pPONService = factory.CreateScope().ServiceProvider.GetRequiredService<IPPONService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.ScheduleInMinutes * 60000;
        var oneTimeValidationSwitch = _appSettings.OneTimeJobSettings.Switch;
        if (oneTimeValidationSwitch && ranOnce)
        {
          _logger.LogInformation("One time validation ran already. Skipping this iteration.");
        }
        else if (oneTimeValidationSwitch)
        {
          bool isDateValid = ValidateDate();
          if (isDateValid)
          {
            await PerformJob(oneTimeValidationSwitch);
          }
        }
        await Task.Delay(interval, stoppingToken);
      }
    }

    private bool ValidateDate()
    {
      var isDateValid = true;
      var startDateString = _appSettings.OneTimeJobSettings.StartDate;
      var endDateString = _appSettings.OneTimeJobSettings.EndDate;

      if (startDateString == null || endDateString == null)
      {
        _logger.LogError("One time validation needs start and end date. Skipping this iteration.");
        isDateValid = false;
      }

      if (isDateValid)
      {
        isDateValid = ConvertDate(startDateString, endDateString);
      }

      return isDateValid;
    }

    private bool ConvertDate(string? startDateString, string? endDateString)
    {
      var isDateValid = true;

      try
      {
        startDate = DateTime.ParseExact(startDateString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(endDateString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
      }
      catch (FormatException)
      {
        _logger.LogError("{0} or {1} is not in the correct format. Date format should be as follows 'yyyy-MM-dd HH:mm' Skipping this iteration.", startDateString, endDateString);        
        isDateValid = false;
      }
      catch (Exception)
      {
        _logger.LogError("Error while reading the start or end date {0}, {1}. Skipping this iteration.", startDateString, endDateString);
        isDateValid = false;
      }

      return isDateValid;
    }

    private async Task PerformJob(bool oneTimeValidationSwitch)
    {
      _logger.LogInformation("");
      _logger.LogInformation("One time validation job switched on. So it runs once to process all the organisation between dates");

      _logger.LogInformation("PPON one time job started at: {time}", DateTimeOffset.Now);

      await _pPONService.PerformJob(oneTimeValidationSwitch, startDate, endDate);
      ranOnce = true;

      _logger.LogInformation("PPON one time job Finsied at: {time}", DateTimeOffset.Now);
      _logger.LogInformation("");
    }
  }
}
