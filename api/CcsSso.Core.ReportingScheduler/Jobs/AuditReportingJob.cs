using CcsSso.Core.ReportingScheduler.Models;
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
                int interval = _appSettings.ScheduleJobSettings.UserReportingJobScheduleInMinutes * 60000; //15000;

                _logger.LogInformation("Audit Reporting Job  running at: {time}", DateTimeOffset.Now);
                await PerformJob();
                await Task.Delay(interval, stoppingToken);

                Console.WriteLine($"******************Audit batch processing job ended ***********");
                Console.WriteLine("");

            }
        }
        private async Task PerformJob()
        {
            try
            {

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
                // var splitedOrgBatch = listOfAllModifiedOrg.GroupBy(s => i++ / size).Select(s => s.ToArray()).ToArray();
                List<AuditLogResponseInfo> userDetailListForAuditLog = new List<AuditLogResponseInfo>();
                AuditLogResponseInfo userAuditLog = null;

                foreach (var eachModifiedAuditLog in listOfAllModifiedAuditLog)
                {
                    index++;
                    _logger.LogInformation($"trying to get user details of {index} nd users");

                    try
                    {
                        try
                        {                            
                            if (eachModifiedAuditLog != null)
                            {
                                userAuditLog = new AuditLogResponseInfo();
                                userAuditLog.Id = eachModifiedAuditLog.Item1;
                                userAuditLog.Event = eachModifiedAuditLog.Item2;
                                userAuditLog.UserId = eachModifiedAuditLog.Item3;
                                userAuditLog.Application = eachModifiedAuditLog.Item4;
                                userAuditLog.ReferenceData = eachModifiedAuditLog.Item5;
                                userAuditLog.IpAddress = eachModifiedAuditLog.Item6;
                                userAuditLog.Device = eachModifiedAuditLog.Item7;                               
                                userDetailListForAuditLog.Add(userAuditLog);
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($" XXXXXXXXXXXX Failed to retrieve user details from Wrapper Api. UserId ={eachModifiedAuditLog.Item3} and Message - {ex.Message} XXXXXXXXXXXX");
                        }

                        if (listOfAllModifiedAuditLog.Count != index && userDetailListForAuditLog.Count < size)
                        {
                            continue;
                        }

                        _logger.LogInformation($"Total number of Audit Logs in this Batch => {userDetailListForAuditLog.Count()}");

                        var fileByteArray = _csvConverter.ConvertToCSV(userDetailListForAuditLog, "audit");

                        _logger.LogInformation("After converting the list of Audit Log object into CSV format and returned byte Array");

                        AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "audit");
                        _logger.LogInformation("After Transfered the files to Azure Blob");

                        if (result.responseStatus)
                        {
                            _logger.LogInformation($"****************** Successfully transfered file. FileName - {result.responseFileName} ******************");
                            Console.WriteLine("");
                        }
                        else
                        {
                            _logger.LogError($" XXXXXXXXXXXX Failed to transfer. Message - {result.responseMessage} XXXXXXXXXXXX");
                            _logger.LogError($"Failed to transfer. File Name - {result.responseFileName}");
                            Console.WriteLine("");

                        }

                    }
                    catch (Exception)
                    {

                        _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of org in this set {userDetailListForAuditLog.Count()} XXXXXXXXXXXX");
                        Console.WriteLine("");

                    }
                    userDetailListForAuditLog.Clear();
                    await Task.Delay(5000);

                }
            }
            catch (Exception ex)
            {

                _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
                Console.WriteLine("");
            }
        }
        public async Task<List<Tuple<int, string, string, string, string, string, string>>> GetModifiedAuditLog()
        {
            var dataDuration = _appSettings.ReportDataDurations.UserReportingDurationInMinutes;
            var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

            try
            {
                string userEmailID = string.Empty;
                var auditLogResult = await (from a in _dataContext.AuditLog
                                            join s in _dataContext.User
                                            on a.UserId equals s.Id
                                            select new Tuple<int, string, string, string, string, string, string>(a.Id,a.Event,s.UserName, a.Application, a.ReferenceData, a.IpAddress, a.Device)
                                            ).ToListAsync();
                /*
                var auditLogResult = await  _dataContext.AuditLog.Where(m => m.EventTimeUtc > untilDateTime)
                                  .Select(a => new Tuple<int, string,int,string,string,string,string>(a.Id, a.Event, a.UserId, a.Application, a.ReferenceData, a.IpAddress, a.Device)).ToListAsync();
                */
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
