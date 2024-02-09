using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Core.ReportingScheduler.Wrapper.Contracts;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using AuditLogResponseInfo = CcsSso.Shared.Domain.Dto.AuditLogResponseInfo;

namespace CcsSso.Core.ReportingScheduler.Jobs
{
  public class AuditReportingJob : BackgroundService
  {

    private readonly ILogger<AuditReportingJob> _logger;
    private readonly AppSettings _appSettings;
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICSVConverter _csvConverter;
    private readonly IFileUploadToCloud _fileUploadToCloud;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly IWrapperContactService _wrapperContactService;


    public AuditReportingJob(IServiceScopeFactory factory, ILogger<AuditReportingJob> logger,
       IDateTimeService dataTimeService, AppSettings appSettings, IHttpClientFactory httpClientFactory,
       ICSVConverter csvConverter, IFileUploadToCloud fileUploadToCloud, IWrapperUserService wrapperUserService,
       IWrapperContactService wrapperContactService, IWrapperOrganisationService wrapperOrganisationService)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _csvConverter = csvConverter;
      _fileUploadToCloud = fileUploadToCloud;
      _wrapperUserService = wrapperUserService;
      _wrapperContactService = wrapperContactService;
      _wrapperOrganisationService = wrapperOrganisationService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.AuditLogReportingJobScheduleInMinutes * 60000; //15000;

        _logger.LogInformation("Audit Reporting Job  running at: {time}", DateTimeOffset.Now);
        await PerformJob();
        _logger.LogInformation("Audit Reporting Job  finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");
        await Task.Delay(interval, stoppingToken);
      }
    }
    private async Task PerformJob()
    {
      try
      {
        var totalNumberOfItemsDuringThisSchedule = 0;

        var auditLogResponse = await GetModifiedAuditLog();



        var listOfAllModifiedAuditLog = auditLogResponse.AuditLogDetail;

        if (listOfAllModifiedAuditLog == null || listOfAllModifiedAuditLog.Count() == 0)
        {
          _logger.LogInformation("No Audit Logs are found");
          return;
        }

        _logger.LogInformation($"Total number of Audit Logs => {listOfAllModifiedAuditLog.Count()}");

        // spliting the jobs
        int size = _appSettings.MaxNumbeOfRecordInAReport;
        _logger.LogInformation($"Max number of record in a report from configuartion settings => {_appSettings.MaxNumbeOfRecordInAReport}");
        var index = 0;

        List<AuditLogResponseInfo> auditLogList = new List<AuditLogResponseInfo>();
        AuditLogResponseInfo auditLog = null;

        foreach (var eachModifiedAuditLog in listOfAllModifiedAuditLog)
        {
          index++;
          _logger.LogInformation($"trying to get audit details of {index}");

          try
          {
            if (eachModifiedAuditLog != null)
            {
              auditLog = new AuditLogResponseInfo();
              auditLog.Id = eachModifiedAuditLog.Id;
              auditLog.Event = eachModifiedAuditLog.Event;
              auditLog.UserId = string.IsNullOrEmpty(eachModifiedAuditLog.UserName) ? "direct api call" : eachModifiedAuditLog.UserName;
              auditLog.Application = eachModifiedAuditLog.Application;
              auditLog.ReferenceData = eachModifiedAuditLog.ReferenceData;
              auditLog.IpAddress = eachModifiedAuditLog.IpAddress;
              auditLog.Device = eachModifiedAuditLog.Device;
              auditLog.EventTimeUtc = eachModifiedAuditLog.EventTimeUtc;
              auditLogList.Add(auditLog);
            }

            if (listOfAllModifiedAuditLog.Count != index && auditLogList.Count < size)
            {
              continue;
            }

            _logger.LogInformation($"Total number of Audit Logs in this Batch => {auditLogList.Count()}");
            totalNumberOfItemsDuringThisSchedule += auditLogList.Count();

            
            var fileByteArray = _csvConverter.ConvertToCSV(auditLogList, "audit");

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

            _logger.LogInformation("After converting the list of Audit Log object into CSV format and returned byte Array");


            AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "Audit");
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

            _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of audit in this set {auditLogList.Count()} XXXXXXXXXXXX");
            _logger.LogError("");

          }
          auditLogList.Clear();
          await Task.Delay(5000);

        }
        _logger.LogInformation($"Total number of audit exported during this schedule => {totalNumberOfItemsDuringThisSchedule}");
      }
      catch (Exception ex)
      {

        _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
        _logger.LogError("");
      }
    }

    public async Task<AuditLogResponse> GetModifiedAuditLog()
    {
      AuditLogResponse auditLogList = new ();
      auditLogList.AuditLogDetail = new();

      var userAuditList= await GetModifiedUserAuditLog();
      var contactAuditList = await GetModifiedContactAuditLog();
      var orgAuditList = await GetModifiedOrgAuditLog();

      auditLogList.AuditLogDetail.AddRange(userAuditList.AuditLogDetail);
      auditLogList.AuditLogDetail.AddRange(contactAuditList.AuditLogDetail);
      auditLogList.AuditLogDetail.AddRange(orgAuditList.AuditLogDetail);

      return auditLogList;
    }


    public async Task<AuditLogResponse> GetModifiedUserAuditLog()
    {
      var dataDuration = _appSettings.ReportDataDurations.AuditLogReportingDurationInMinutes;
      DateTime untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      AuditLogResponse auditLogResult =new();
      int currentPage = 1;
      int pageSize = 50;

      try
      {
        auditLogResult = await _wrapperUserService.GetUserAuditLog(untilDateTime, pageSize, currentPage);

        for (int i = 2; i <= auditLogResult.PageCount; i++)
        {
          var auditLogNextPageResult = await _wrapperUserService.GetUserAuditLog(untilDateTime, pageSize, i);
          auditLogResult.CurrentPage = i;
          auditLogResult.AuditLogDetail.AddRange(auditLogNextPageResult.AuditLogDetail);
        }

        return auditLogResult;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }

    public async Task<AuditLogResponse> GetModifiedOrgAuditLog()
    {
      var dataDuration = _appSettings.ReportDataDurations.AuditLogReportingDurationInMinutes;
      DateTime untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      AuditLogResponse auditLogResult = new();
      int currentPage = 1;
      int pageSize = 50;

      try
      {
        auditLogResult = await _wrapperOrganisationService.GetOrgAuditLog(untilDateTime, pageSize, currentPage);
        await AddUserName(auditLogResult);

        for (int i = 2; i <= auditLogResult.PageCount; i++)
        {
          var auditLogNextPageResult = await _wrapperOrganisationService.GetOrgAuditLog(untilDateTime, pageSize, i);
          await AddUserName(auditLogNextPageResult);

          auditLogResult.CurrentPage = i;
          auditLogResult.AuditLogDetail.AddRange(auditLogNextPageResult.AuditLogDetail);
        }

        return auditLogResult;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }

    private async Task AddUserName(AuditLogResponse auditLogResult)
    {
      if (!auditLogResult.AuditLogDetail.Any())
        return;

      var listOfUserIds = auditLogResult.AuditLogDetail.Select(x => x.UserId).ToList().Distinct();
      var listOfUserNames = await _wrapperUserService.GetUserNames(string.Join(",", listOfUserIds));

      foreach (var item in auditLogResult.AuditLogDetail)
      {
        item.UserName = listOfUserNames.UserNameList.FirstOrDefault(x => x.Id == item.UserId)?.Name;
      }
    }

    public async Task<AuditLogResponse> GetModifiedContactAuditLog()
    {
      var dataDuration = _appSettings.ReportDataDurations.AuditLogReportingDurationInMinutes;
      DateTime untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      AuditLogResponse auditLogResult = new();
      int currentPage = 1;
      int pageSize = 50;

      try
      {
        auditLogResult = await _wrapperContactService.GetContactAuditLog(untilDateTime, pageSize, currentPage);
        await AddUserName(auditLogResult);


        for (int i = 2; i <= auditLogResult.PageCount; i++)
        {
          var auditLogNextPageResult = await _wrapperContactService.GetContactAuditLog(untilDateTime, pageSize, i);
          await AddUserName(auditLogNextPageResult);

          auditLogResult.CurrentPage = i;
          auditLogResult.AuditLogDetail.AddRange(auditLogNextPageResult.AuditLogDetail);
        }

        return auditLogResult;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }
  }
}
