using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserProfileResponseInfo = CcsSso.Shared.Domain.Dto.UserProfileResponseInfo;

namespace CcsSso.Core.ReportingScheduler.Jobs
{
    public class UserReportingJob : BackgroundService
    {

        private readonly ILogger<UserReportingJob> _logger;
        private readonly AppSettings _appSettings;
        private readonly IDataContext _dataContext;
        private readonly IDateTimeService _dataTimeService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICSVConverter _csvConverter;
        private readonly IFileUploadToCloud _fileUploadToCloud;

        public UserReportingJob(IServiceScopeFactory factory, ILogger<UserReportingJob> logger,
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

                _logger.LogInformation("User Reporting Job  running at: {time}", DateTimeOffset.Now);
                await PerformJob();
                await Task.Delay(interval, stoppingToken);

                Console.WriteLine($"******************User batch processing job ended ***********");
                Console.WriteLine("");

            }
        }
        private async Task PerformJob()
        {
            try
            {

                var listOfAllModifiedUser = await GetModifiedUserIds();

                if (listOfAllModifiedUser == null || listOfAllModifiedUser.Count() == 0)
                {
                    _logger.LogInformation("No User found");
                    return;
                }

                _logger.LogInformation($"Total number of Users => {listOfAllModifiedUser.Count()}");

                // spliting the jobs
                int size = _appSettings.MaxNumbeOfRecordInAReport;
                _logger.LogInformation($"Max number of record in a report from configuartion settings => {_appSettings.MaxNumbeOfRecordInAReport}");
                var index = 0;
                // var splitedOrgBatch = listOfAllModifiedOrg.GroupBy(s => i++ / size).Select(s => s.ToArray()).ToArray();
                List<UserProfileResponseInfo> userDetailList = new List<UserProfileResponseInfo>();

                foreach (var eachModifiedUser in listOfAllModifiedUser)
                {
                    index++;
                    _logger.LogInformation($"trying to get user details of {index} nd users");

                    try
                    {
                        try
                        {
                            _logger.LogInformation("Calling wrapper API to get User Details");
                            var client = _httpClientFactory.CreateClient("WrapperApi");
                            var userDetails = await GetUserDetails(eachModifiedUser, client);
                            if (userDetails != null)
                            {
                                userDetailList.Add(userDetails);
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($" XXXXXXXXXXXX Failed to retrieve user details from Wrapper Api. UserId ={eachModifiedUser.Item2} and Message - {ex.Message} XXXXXXXXXXXX");
                        }

                        if (listOfAllModifiedUser.Count != index && userDetailList.Count < size)
                        {
                            continue;
                        }

                        _logger.LogInformation($"Total number of Users in this Batch => {userDetailList.Count()}");

                        var fileByteArray = _csvConverter.ConvertToCSV(userDetailList, "user");

                        _logger.LogInformation("After converting the list of user object into CSV format and returned byte Array");

                        AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "user");
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

                        _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of org in this set {userDetailList.Count()} XXXXXXXXXXXX");
                        Console.WriteLine("");

                    }
                    userDetailList.Clear();
                    await Task.Delay(5000);

                }
            }
            catch (Exception ex)
            {

                _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
                Console.WriteLine("");
            }
        }

        private async Task<UserProfileResponseInfo?> GetUserDetails(Tuple<int, string> eachModifiedUser, HttpClient client)
        {
            string url = $"users/?user-id={eachModifiedUser.Item2}"; // Send as Query String as expected in the Wrapper API - GetUser method

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Retrived user details for userId-{eachModifiedUser.Item2}");

                return JsonConvert.DeserializeObject<UserProfileResponseInfo>(content);           

            }
            else
            {
                _logger.LogError($"No Users retrived for userId-{eachModifiedUser.Item2}");
                return null;
            }
        }
        public async Task<List<Tuple<int, string>>> GetModifiedUserIds()
        {
            var dataDuration = _appSettings.ReportDataDurations.UserReportingDurationInMinutes;
            var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

            try
            {
                var userIds = await _dataContext.User.Where(
                                  usr => !usr.IsDeleted && usr.LastUpdatedOnUtc > untilDateTime)
                                  .Select(u => new Tuple<int, string>(u.Id, u.UserName)).ToListAsync();
                return userIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                throw;
            }


        }
    }
}

