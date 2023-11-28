using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CcsSso.Core.Domain.Contracts.Wrapper;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Web;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Dtos.Domain.Models;
using Newtonsoft.Json;
using CcsSso.Domain.Exceptions;
using System.Net;

namespace CcsSso.Core.DormancyJobScheduler.Services
{
  public class UserDeactivationService : IUserDeactivationService
  {
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IUserDeactivationService> _logger;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DormancyAppSettings _appSettings;
    private readonly IAuth0Service _auth0Service;

    public UserDeactivationService(IDateTimeService dateTimeService, ILogger<IUserDeactivationService> logger, IWrapperUserService wrapperUserService,
      IHttpClientFactory httpClientFactory, DormancyAppSettings dormancyAppSettings, IAuth0Service auth0Service)

    {
      _dateTimeService = dateTimeService;
      _wrapperUserService = wrapperUserService;
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _appSettings = dormancyAppSettings;
      _auth0Service = auth0Service;
    }

    #region UserDeactivation job
    public async Task PerformUserDeactivationJobAsync()
    {
      //var userDeactivationList = await GetUserDeactivationList();
      int page = 1;
      int pageSize = 100;
      decimal totalPages = 1;
      decimal total = 0;
      DateTime currentDate = _dateTimeService.GetUTCNow();
      string fromDate = string.Empty, toDate = string.Empty;
      if (_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes < 1440) //for testing purpose only
      {
        fromDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes)
          .AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToUniversalTime()
                         .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        toDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToUniversalTime()
                         .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
      }
      else
      {
        // Calculate the date 12 months ago
        fromDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddDays(-1).ToString("yyyy-MM-dd");
        toDate = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddDays(-1).ToString("yyyy-MM-dd");
      }

      _logger.LogInformation($"Dormant notification From Date: {fromDate}, To Date: {toDate}");
      try
      {
        do
        {
          UserListDetails userDetails = await _auth0Service.GetUsersByLastLogin(fromDate, toDate, page - 1, pageSize);
          total = userDetails.Total;
          totalPages = Math.Ceiling(total / pageSize);
          page++;
          DeactivateUsers(userDetails);

        } while (page <= totalPages);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting user deactivation list, exception message =  {ex.Message}");
      }

    }

    public async void DeactivateUsers(UserListDetails userDetails)
    {
      if (userDetails != null)
      {
        if (!userDetails.Users.Any())
        {
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          _logger.LogInformation("No users found for Deactivation.");
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          return;
        }

        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation($"User Deactivation list count: {userDetails.Users.Count()}");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        foreach (var user in userDetails.Users)
        {
          try
          {
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            _logger.LogInformation($"User to be dormanted: {user.Email}");
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            //await _wrapperUserService.DeactivateUserAsync(user.Email, CcsSso.Domain.Constants.DormantBy.Job);
          }
          catch (Exception ex)
          {
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            _logger.LogInformation($"User Deactivation failed for the user: {user.Email}");
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          }
        }
      }
    }

    #endregion

  }
}
