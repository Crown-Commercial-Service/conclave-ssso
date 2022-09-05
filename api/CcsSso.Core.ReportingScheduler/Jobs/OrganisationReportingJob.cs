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

        _logger.LogInformation("Organisation Reporting Job  started at: {time}", DateTimeOffset.Now);
        await PerformJob();

        _logger.LogInformation("Organisation Reporting Job  finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");

        await Task.Delay(interval, stoppingToken);

        

      }
    }

    private async Task PerformJob()
    {
      try
      {
        var totalNumberOfItemsDuringThisSchedule = 0;
        var listOfAllModifiedOrg = await GetModifiedOrganisationIds();

        if (listOfAllModifiedOrg == null || listOfAllModifiedOrg.Count() == 0)
        {
          _logger.LogInformation("No Organisation found");
          return;
        }

        _logger.LogInformation($"Total number of Orgs => {listOfAllModifiedOrg.Count()}");

        // spliting the jobs
        int size = _appSettings.MaxNumbeOfRecordInAReport;
        _logger.LogInformation($"Max number of record in a report from configuartion settings => {_appSettings.MaxNumbeOfRecordInAReport}");
        var index = 0;
        
        List<OrganisationProfileResponseInfo> orgDetailList = new List<OrganisationProfileResponseInfo>();

        foreach (var eachModifiedOrg in listOfAllModifiedOrg)
        {
          index++;
          _logger.LogInformation($"trying to get organisation details of {index}");

          try
          {

            try
            {
              _logger.LogInformation("Calling wrapper API to get Organisation Details");
              var client = _httpClientFactory.CreateClient("WrapperApi");
              var orgDetails = await GetOrganisationDetails(eachModifiedOrg, client);
              if (orgDetails != null)
              {
                orgDetailList.Add(orgDetails);
              }

            }
            catch (Exception ex)
            {

              _logger.LogError($" XXXXXXXXXXXX Failed to retrieve organisation details from Wrapper Api. OrganisationId ={eachModifiedOrg.Item2} and Message - {ex.Message} XXXXXXXXXXXX");
            }

            if(listOfAllModifiedOrg.Count != index &&  orgDetailList.Count < size)
            {
              continue;
            }

            _logger.LogInformation($"Total number of Orgs in this Batch => {orgDetailList.Count()}");
            totalNumberOfItemsDuringThisSchedule += orgDetailList.Count();


          _logger.LogInformation("After calling the wrapper API to get Organisation Details");

            var fileByteArray = _csvConverter.ConvertToCSV(orgDetailList, "organisation");

            if (_appSettings.WriteCSVDataInLog)
            {
              try
              {
                MemoryStream ms = new MemoryStream(fileByteArray);
                StreamReader reader = new StreamReader(ms);
                string csvData = reader.ReadToEnd();
                _logger.LogInformation("CSV Data as follows");
                _logger.LogInformation(csvData);
                _logger.LogInformation("");
              }
              catch (Exception ex)
              {
                _logger.LogWarning($"It is temporary logging to check the csv data which through exception -{ex.Message}");
              }
            }

            _logger.LogInformation("After converting the list of organisation object into CSV format and returned byte Array");

            AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "Organisation");
            _logger.LogInformation("After Transfered the files to Azure Blob");

            if (result.responseStatus)
            {
              _logger.LogInformation($"****************** Successfully transfered file. FileName - {result.responseFileName} ******************");
              _logger.LogInformation("");
            }
            else
            {
              _logger.LogError($" XXXXXXXXXXXX Failed to transfer. Message - {result.responseMessage} XXXXXXXXXXXX");
              _logger.LogError($"Failed to transfer. File Name - {result.responseFileName}");
              _logger.LogInformation("");

            }

          }
          catch (Exception)
          {

            _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of org in this set {orgDetailList.Count()} XXXXXXXXXXXX");
            _logger.LogError("");

          }
          orgDetailList.Clear();
          await Task.Delay(5000);

        }
        
          _logger.LogInformation($"Total number of organisation exported during this schedule => {totalNumberOfItemsDuringThisSchedule}");
      }
      catch (Exception ex)
      {

        _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
        _logger.LogError("");
      }
    }

    private async Task<OrganisationProfileResponseInfo?> GetOrganisationDetails(Tuple<int, string> eachModifiedOrg, HttpClient client)
    {
      string url = $"organisations/{eachModifiedOrg.Item2}";
      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived org details for orgId-{eachModifiedOrg.Item2}");

        return JsonConvert.DeserializeObject<OrganisationProfileResponseInfo>(content);


      }
      else
      {
        _logger.LogError($"No organisation retrived for orgId-{eachModifiedOrg.Item2}");
        return null;
      }

    }

    public async Task<List<Tuple<int, string>>> GetModifiedOrganisationIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.OrganisationReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsDeleted && org.LastUpdatedOnUtc > untilDateTime)
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
