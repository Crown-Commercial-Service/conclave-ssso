using Amazon.Runtime;
using CcsSso.Core.ServiceOnboardingScheduler.Model;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CcsSso.Core.ServiceOnboardingScheduler.Jobs
{
  public class CASOnboardingJob : BackgroundService
  {
    private readonly OnBoardingAppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;
    private readonly IDataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;


    private readonly ILogger<CASOnboardingJob> _logger;

    public CASOnboardingJob(ILogger<CASOnboardingJob> logger, OnBoardingAppSettings appSettings, IDateTimeService dataTimeService,
       IHttpClientFactory httpClientFactory, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;

      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.CASOnboardingJobScheduleInMinutes * 60000;

        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        await PerformJob();

        await Task.Delay(interval, stoppingToken);
      }
    }

    private async Task PerformJob()
    {
      try
      {
        var listOfRegisteredOrgs = await GetRegisteredOrgsIds();


        if (listOfRegisteredOrgs == null || listOfRegisteredOrgs.Count() == 0)
        {
          _logger.LogInformation("No Organisation found");
          return;
        }
        var index = 0;

        foreach (var eachOrgs in listOfRegisteredOrgs)
        {
          index++;
          _logger.LogInformation($"trying to get adminDetail details of {index}");
          _logger.LogInformation($"OrgName {eachOrgs.Item3}");


        }

        var result = await IsValidBuyer("brickendon.com");

        _logger.LogInformation($"Autovalidation result {result}");


      }
      catch (Exception)
      {

        throw;
      }
    }

    private async Task<List<Tuple<int, string,string>>> GetRegisteredOrgsIds()
    {
      var dataDuration = _appSettings.OnBoardingDataDuration.CASOnboardingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsDeleted && org.RightToBuy==false && org.SupplierBuyerType==0 // ToDo: change to buyer or both

                          && org.LastUpdatedOnUtc > untilDateTime)
                          .Select(o => new Tuple<int, string,string>(o.Id, o.CiiOrganisationId,o.LegalName)).ToListAsync();
        return organisationIds;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }


    }


    private async Task<bool> IsValidBuyer(string domain)
    {
      var client = _httpClientFactory.CreateClient("LookupApi");
      var url = "/lookup?domainname=" + domain; 
      var response = await client.GetAsync(url); 
      if (response.StatusCode == System.Net.HttpStatusCode.OK) 
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        var isValid = JsonConvert.DeserializeObject<bool>(responseContent); 
        return isValid; 
      }

      return false;
    }



  }

}
