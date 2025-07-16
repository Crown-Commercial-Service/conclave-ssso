using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Core.ReportingScheduler.Wrapper.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Web;
using UserProfileResponseInfo = CcsSso.Shared.Domain.Dto.UserProfileResponseInfo;

namespace CcsSso.Core.ReportingScheduler.Jobs
{
  public class UserReportingJob : BackgroundService
  {

    private readonly ILogger<UserReportingJob> _logger;
    private readonly AppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICSVConverter _csvConverter;
    private readonly IFileUploadToCloud _fileUploadToCloud;
    private readonly IWrapperUserService _wrapperUserService;

    public UserReportingJob(IServiceScopeFactory factory, ILogger<UserReportingJob> logger,
       IDateTimeService dataTimeService, AppSettings appSettings, IHttpClientFactory httpClientFactory,
       ICSVConverter csvConverter, IFileUploadToCloud fileUploadToCloud
       , IWrapperUserService wrapperUserService)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _csvConverter = csvConverter;
      _fileUploadToCloud = fileUploadToCloud;
      _wrapperUserService = wrapperUserService;

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.UserReportingJobScheduleInMinutes * 60000; //15000;

        _logger.LogInformation("User Reporting Job  running at: {time}", DateTimeOffset.Now);
        await PerformJob();

        _logger.LogInformation("User Reporting Job  finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");

        await Task.Delay(interval, stoppingToken);


      }
    }
    private async Task PerformJob()
    {
      try
      {
        var totalNumberOfItemsDuringThisSchedule = 0;

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

        List<UserProfileResponseInfo> userDetailList = new List<UserProfileResponseInfo>();

        foreach (var eachModifiedUser in listOfAllModifiedUser)
        {
          index++;
          _logger.LogInformation($"trying to get user details of {index}");

          try
          {
            try
            {
              _logger.LogInformation("Calling wrapper API to get User Details");
              var client = _httpClientFactory.CreateClient("UserWrapperApi");
              var userDetails = await GetUserDetails(eachModifiedUser, client);
              if (userDetails != null)
              {
                userDetailList.Add(userDetails);
              }

            }
            catch (Exception ex)
            {
              _logger.LogError($" XXXXXXXXXXXX Failed to retrieve user details from Wrapper Api. UserId ={eachModifiedUser.UserName} and Message - {ex.Message} XXXXXXXXXXXX");
            }

            if (listOfAllModifiedUser.Count != index && userDetailList.Count < size)
            {
              continue;
            }

            _logger.LogInformation($"Total number of Users in this Batch => {userDetailList.Count()}");
            totalNumberOfItemsDuringThisSchedule += userDetailList.Count();

            _logger.LogInformation("After calling the wrapper API to get User Details");

            var fileByteArray = _csvConverter.ConvertToCSV(userDetailList, "user");

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

            _logger.LogInformation("After converting the list of user object into CSV format and returned byte Array");

            AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "User");
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

            _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of users in this set {userDetailList.Count()} XXXXXXXXXXXX");
            _logger.LogError("");

          }
          userDetailList.Clear();
          await Task.Delay(5000);

        }
        _logger.LogInformation($"Total number of users exported during this schedule => {totalNumberOfItemsDuringThisSchedule}");
      }
      catch (Exception ex)
      {

        _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
        _logger.LogError("");
      }
    }

    private async Task<UserProfileResponseInfo?> GetUserDetails(UserReportDetail eachModifiedUser, HttpClient client)
    {
      string url = $"?user-id={HttpUtility.UrlEncode(eachModifiedUser.UserName)}"; // Send as Query String as expected in the Wrapper API - GetUser method

      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived user details for userId-{eachModifiedUser.UserName}");

        return JsonConvert.DeserializeObject<UserProfileResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for userId-{eachModifiedUser.UserName}");
        return null;
      }
    }
    public async Task<List<UserReportDetail>> GetModifiedUserIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.UserReportingDurationInMinutes;

      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var detectedUsers = _wrapperUserService.GetModifiedUsers(untilDateTime.ToString("MM-dd-yyyy HH:mm:ss")).Result;
        return detectedUsers;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }


    }
  }
}

