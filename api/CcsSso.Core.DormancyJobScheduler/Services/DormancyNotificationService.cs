using Amazon.S3;
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System.Net;
using CcsSso.Domain.Exceptions;

namespace CcsSso.Core.DormancyJobScheduler.Services
{
  public class DormancyNotificationService : IDormancyNotificationService
  {
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IDormancyNotificationService> _logger;
    private readonly DormancyAppSettings _appSettings;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailProviderService _emaillProviderService;

    public DormancyNotificationService(IDateTimeService dateTimeService,
      DormancyAppSettings appSettings,
      ILogger<IDormancyNotificationService> logger,
      IWrapperUserService wrapperUserService, IHttpClientFactory httpClientFactory, IEmailProviderService emaillProviderService)
    {
      _dateTimeService = dateTimeService;
      _appSettings = appSettings;
      _wrapperUserService = wrapperUserService;
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _emaillProviderService = emaillProviderService;
    }
    public async Task PerformDormancyNotificationJobAsync()
    {
      var usersWithinExpiredNotice = await GetUsersWithinExpiredNotice();
      if (!usersWithinExpiredNotice.Any())
      {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation("No users found for  dormant notification.");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        return;
      }
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      _logger.LogInformation($"User Dormant Notification list count: {usersWithinExpiredNotice.Count()}");
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      try
      {
        foreach (var user in usersWithinExpiredNotice)
        {
          _logger.LogInformation($"User Detail to send Notification:{user}");
          var userDetail = await _wrapperUserService.GetUserDetails(user);
          if(userDetail==null)
          {
            throw new Exception("Error fetching user Details"+userDetail.UserName);
          }
          if (!userDetail.IsDormant)
          {
            var userEmailInfo = getDormantNotificationEmailInfo(user);
            _emaillProviderService.SendEmailAsync(userEmailInfo);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogInformation($"User Dormant Notification failed: {ex.Message}");
      }
    }

   
    private EmailInfo getDormantNotificationEmailInfo(string toEmailId)
    {
      var emailTempalteId = _appSettings.EmailSettings.UserDormantNotificationTemplateId;

      var emailInfo = new EmailInfo
      {
        To = toEmailId,
        TemplateId = emailTempalteId,
      };

      return emailInfo;
    }
    private async Task<List<string>> GetUsersWithinExpiredNotice()
    {
      var usersNeedNotificationEmail = new List<string>();
      try
      {
        DateTime currentDate =_dateTimeService.GetUTCNow();
        //Get the notify duration
        int notifyduration = _appSettings.DormancyJobSettings.DeactivationNotificationInMinutes;
        // Calculate the date 12 months ago and add 7 days to send the user Notification
        string dormantNotifyDuration = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(notifyduration).AddDays(-1).ToString("yyyy-MM-dd");
        _logger.LogInformation($"User Dormant Notification Date: {dormantNotifyDuration}");
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);
        var url = "security/data/users?date=" + HttpUtility.UrlEncode(dormantNotifyDuration) + "&is-exact=true";
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          var result = JsonConvert.DeserializeObject<List<string>>(content);
          usersNeedNotificationEmail = result;
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
          throw new ResourceNotFoundException();
        }
        else
        {
          throw new CcsSsoException("ERROR_RETRIEVING_USER_DETAILS_FOR_DORMANT_NOTIFICATION");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting users Deactivation Notice list, exception message =  {ex.Message}");
      }
      return usersNeedNotificationEmail;
    }
  }

}