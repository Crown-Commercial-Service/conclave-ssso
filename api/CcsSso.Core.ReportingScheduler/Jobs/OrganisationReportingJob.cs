using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrganisationProfileResponseInfo = CcsSso.Shared.Domain.Dto.OrganisationProfileResponseInfo;


namespace CcsSso.Core.ReportingScheduler.Jobs
{
  public class OrganisationReportingJob : BackgroundService
  {
    private readonly ILogger<OrganisationReportingJob> _logger;
    private readonly AppSettings _appSettings;
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICSVConverter _csvConverter;
    private readonly IFileUploadToCloud _fileUploadToCloud;




    public OrganisationReportingJob(IServiceScopeFactory factory, ILogger<OrganisationReportingJob> logger,
       IDateTimeService dataTimeService, AppSettings appSettings, IHttpClientFactory httpClientFactory,
       ICSVConverter csvConverter, IFileUploadToCloud fileUploadToCloud)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _csvConverter = csvConverter;
      _fileUploadToCloud = fileUploadToCloud;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = 15000;// _appSettings.ScheduleJobSettings.OrganisationReportingJobScheduleInMinutes * 60000;

        _logger.LogInformation("Organisation Reporting Job  running at: {time}", DateTimeOffset.Now);
        await PerformJob();
        await Task.Delay(interval, stoppingToken);

        Console.WriteLine($"******************Organization batch processing job ended ***********");

      }
    }

    private async Task PerformJob()
    {
      var listOfModifiedOrg = await GetExpiredOrganisationIdsAsync();

      if (listOfModifiedOrg == null || listOfModifiedOrg.Count() == 0)
        return;

      var client = _httpClientFactory.CreateClient("WrapperApi");

      List<OrganisationProfileResponseInfo> orgDetailList = new List<OrganisationProfileResponseInfo>();

      await GetOrganisationDetails(listOfModifiedOrg.Take(2).ToList(), client, orgDetailList);

      var memoryFileObject = _csvConverter.ConvertToCSV(orgDetailList, "organisation");

      var result = _fileUploadToCloud.FileUploadToAzureBlobAsync(memoryFileObject, "", "", "");

      // var result = fileTransfe.performJob(memoryFileObject)

      //  if (result.success)
      //{
      //  _logger.LogInformation("success");
      //}

      // call json converter 

      // file upload to s3

    }

    private async Task GetOrganisationDetails(List<Tuple<int, string>> listOfModifiedOrg, HttpClient client, List<OrganisationProfileResponseInfo> orgDetailList)
    {
      foreach (var orgId in listOfModifiedOrg)
      {
        string url = $"organisations/{orgId.Item2}";
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          var orgInfo = JsonConvert.DeserializeObject<OrganisationProfileResponseInfo>(content);
          orgDetailList.Add(orgInfo);

          _logger.LogInformation($"Retrived org details for orgId-{orgId}");
        }
        else
        {
          _logger.LogInformation($"No organisation retrived for orgId-{orgId}");
        }

      }
    }

    public async Task<List<Tuple<int, string>>> GetExpiredOrganisationIdsAsync()
    {
      var dataDuration = _appSettings.ReportDataDurations.OrganisationReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted
                           && org.CreatedOnUtc < untilDateTime)
                          .Select(o => new Tuple<int, string>(o.Id, o.CiiOrganisationId)).ToListAsync();
        return organisationIds;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
  
      
    }
  }
}
