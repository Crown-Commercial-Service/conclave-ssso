using CcsSso.Core.ServiceOnboardingScheduler.Model;
using CcsSso.Shared.Contracts;

namespace CcsSso.Core.ServiceOnboardingScheduler.Jobs
{
  public class CASOnboardingJob : BackgroundService
  {
    private readonly OnBoardingAppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;

    private readonly ILogger<CASOnboardingJob> _logger;

    public CASOnboardingJob(ILogger<CASOnboardingJob> logger, OnBoardingAppSettings appSettings, IDateTimeService dataTimeService)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.CASOnboardingJobScheduleInMinutes * 60000;

        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        await PerformJob();

        await Task.Delay(1000, stoppingToken);
      }
    }

    private async Task PerformJob()
    {
      try
      {
        //var listOfRegisteredOrgs = await GetRegisteredOrgsIds();


        //if (listOfRegisteredOrgs == null || listOfRegisteredOrgs.Count() == 0)
        //{
        //  _logger.LogInformation("No Organisation found");
        //  return;
        //}
      }
      catch (Exception)
      {

        throw;
      }
    }

    //public async Task<List<Tuple<int, string>>> GetModifiedOrganisationIds()
    //{
    //  var dataDuration = _appSettings.OnBoardingDataDuration.CASOnboardingDurationInMinutes;
    //  var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

    //  try
    //  {
    //    //var organisationIds = await _dataContext.Organisation.Where(
    //    //                  org => !org.IsDeleted && org.LastUpdatedOnUtc > untilDateTime)
    //    //                  .Select(o => new Tuple<int, string>(o.Id, o.CiiOrganisationId)).ToListAsync();
    //    //return organisationIds;
    //  }
    //  catch (Exception ex)
    //  {
    //    _logger.LogError(ex, "Error");
    //    throw;
    //  }


    //}
  }
}
