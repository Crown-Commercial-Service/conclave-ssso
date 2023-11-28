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
    private readonly IAuth0Service _auth0Service;

    public DormancyNotificationService(IDateTimeService dateTimeService,
      DormancyAppSettings appSettings,
      ILogger<IDormancyNotificationService> logger,
      IWrapperUserService wrapperUserService, IHttpClientFactory httpClientFactory, IEmailProviderService emaillProviderService,
      IAuth0Service auth0Service)
    {
      _dateTimeService = dateTimeService;
      _appSettings = appSettings;
      _wrapperUserService = wrapperUserService;
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _emaillProviderService = emaillProviderService;
      _auth0Service = auth0Service;
    }
    public async Task PerformDormancyNotificationJobAsync()
    {
      //var usersWithinExpiredNotice = await GetUsersWithinExpiredNotice();
      int page = 1;
      int pageSize = 100;
      decimal totalPages = 1;
      decimal total = 0;
      string fromDate = string.Empty, toDate = string.Empty;
      DateTime currentDate = _dateTimeService.GetUTCNow();
      //Get the notify duration
      int notifyduration = _appSettings.DormancyJobSettings.DeactivationNotificationInMinutes;

      if (_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes < 1440) //for testing purpose only
      {
        fromDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(-notifyduration)
          .AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToUniversalTime()
                         .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        toDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(-notifyduration).ToUniversalTime()
                         .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
      }
      else
      {
        // Calculate the date 12 months ago and add 7 days to send the user Notification        
        fromDate = toDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(-notifyduration).ToString("yyyy-MM-dd");
      }

      _logger.LogInformation($"Dormant From Date: {fromDate}, To Date: {toDate}");
      try
      {
        do
        {
          UserListDetails userDetails = await _auth0Service.GetUsersByLastLogin(fromDate, toDate, page - 1, pageSize);
          total = userDetails.Total;
          totalPages = Math.Ceiling(total / pageSize);
          page++;
          SendNotification(userDetails);

        } while (page <= totalPages);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting users Deactivation Notice list, exception message =  {ex.Message}");
      }

    }

    public async void SendNotification(UserListDetails userDetails)
    {
      if (userDetails == null)
      {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation("No users found for  dormant notification.");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        return;
      }
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      _logger.LogInformation($"User Dormant Notification list count: {userDetails.Users.Count()}");
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
    
        foreach (var user in userDetails.Users)
        {
        try
        {
          _logger.LogInformation($"User Detail to send Notification:{user.Email}");
          var userDetail = await _wrapperUserService.GetUserDetails(user.Email);
          if (userDetail == null)
          {
            throw new Exception("Error fetching user Details" + user.Email);
          }
          if (!userDetail.IsDormant)
          {
            var userEmailInfo = getDormantNotificationEmailInfo(user.Email);
            _emaillProviderService.SendEmailAsync(userEmailInfo);
          }
        }
        catch (Exception ex)
        {
          _logger.LogInformation($"User Dormant Notification failed: {ex.Message}");
        }
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
  }
}