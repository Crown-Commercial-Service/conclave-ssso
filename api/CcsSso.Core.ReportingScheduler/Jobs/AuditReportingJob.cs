﻿using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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

        public AuditReportingJob(IServiceScopeFactory factory, ILogger<AuditReportingJob> logger,
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

                var listOfAllModifiedAuditLog = await GetModifiedAuditLog();

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
                    _logger.LogInformation($"trying to get audit details of {index} nd audit");

                    try
                    {                                                 
                        if (eachModifiedAuditLog != null)
                        {
                            auditLog = new AuditLogResponseInfo();
                            auditLog.Id = eachModifiedAuditLog.Item1;
                            auditLog.Event = eachModifiedAuditLog.Item2;
                            auditLog.UserId = eachModifiedAuditLog.Item3;
                            auditLog.Application = eachModifiedAuditLog.Item4;
                            auditLog.ReferenceData = eachModifiedAuditLog.Item5;
                            auditLog.IpAddress = eachModifiedAuditLog.Item6;
                            auditLog.Device = eachModifiedAuditLog.Item7;
                            auditLogList.Add(auditLog);
                        }

                        if (listOfAllModifiedAuditLog.Count != index && auditLogList.Count < size)
                        {
                            continue;
                        }

                        _logger.LogInformation($"Total number of Audit Logs in this Batch => {auditLogList.Count()}");
                         totalNumberOfItemsDuringThisSchedule += auditLogList.Count();

                            
                        var fileByteArray = _csvConverter.ConvertToCSV(auditLogList, "audit");
                        
                        _logger.LogInformation("After converting the list of Audit Log object into CSV format and returned byte Array");

                        AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "audit");
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

                        _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of org in this set {auditLogList.Count()} XXXXXXXXXXXX");
                        _logger.LogError("");

                    }
                    auditLogList.Clear();
                    await Task.Delay(5000);

                }
            }
            catch (Exception ex)
            {

                _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
                _logger.LogError("");
            }
        }
        public async Task<List<Tuple<int, string, string, string, string, string, string>>> GetModifiedAuditLog()
        {
            var dataDuration = _appSettings.ReportDataDurations.AuditLogReportingDurationInMinutes;
            var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

            try
            {              
                var auditLogResult = await (from a in _dataContext.AuditLog
                                            join s in _dataContext.User
                                            on a.UserId equals s.Id
                                            select new Tuple<int, string, string, string, string, string, string>(a.Id,a.Event,s.UserName, a.Application, a.ReferenceData, a.IpAddress, a.Device)
                                            ).ToListAsync();
                
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
