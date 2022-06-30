using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        int interval = _appSettings.ScheduleJobSettings.OrganisationReportingJobScheduleInMinutes * 60000; //15000;

        _logger.LogInformation("Organisation Reporting Job  running at: {time}", DateTimeOffset.Now);
        await PerformJob();
        await Task.Delay(interval, stoppingToken);

        Console.WriteLine($"******************Organization batch processing job ended ***********");

      }
    }

    private async Task PerformJob()
    {
      try
      {

        var listOfModifiedOrg = await GetModifiedOrganisationIds();

        if (listOfModifiedOrg == null || listOfModifiedOrg.Count() == 0)
          return;

        _logger.LogInformation($"Trying to transfer the organisation -{string.Join(",", listOfModifiedOrg.Select(x => x.Item2).ToArray())}");


        var client = _httpClientFactory.CreateClient("WrapperApi");
        List<OrganisationProfileResponseInfo> orgDetailList = new List<OrganisationProfileResponseInfo>();
        await GetOrganisationDetails(listOfModifiedOrg, client, orgDetailList);

        _logger.LogInformation("After calling the wrapper API to get Organisation Details");

        var fileByteArray = _csvConverter.ConvertToCSV(orgDetailList, "organisation");

        _logger.LogInformation("After converting the list of organisation object into CSV format and returned byte Array");

        AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "organisation");
        _logger.LogInformation("After Transfered the files to Azure Blob");

        if (result.responseStatus)
        {
          _logger.LogInformation($"Successfully transfered file. FileName - {result.responseFileName}");
        }
        else
        {
          _logger.LogInformation($"Failed to transfer. Message - {result.responseMessage}");
          _logger.LogInformation($"Failed to transfer. File Name - {result.responseFileName}");
        }

      }
      catch (Exception ex)
      {

        _logger.LogInformation($"Failed to transfer. Outer exception - {ex.Message}");
      }


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

    public async Task<List<Tuple<int, string>>> GetModifiedOrganisationIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.OrganisationReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted
                           && org.LastUpdatedOnUtc > untilDateTime)
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
