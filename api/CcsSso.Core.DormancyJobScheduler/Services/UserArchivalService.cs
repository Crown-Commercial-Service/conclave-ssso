using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Services
{
  public class UserArchivalService : IUserArchivalService
  {
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IUserArchivalService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DormancyAppSettings _appSettings;
    private readonly IWrapperUserService _wrapperUserService;

    public UserArchivalService(IDateTimeService dateTimeService, ILogger<IUserArchivalService> logger,
      IHttpClientFactory httpClientFactory, DormancyAppSettings dormancyAppSettings, IWrapperUserService wrapperUserService)

    {
      _dateTimeService = dateTimeService;
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _appSettings = dormancyAppSettings;
      _wrapperUserService = wrapperUserService;
    }
    public async Task PerformUserArchivalJobAsync()
    {
      int page = 1;
      int pageSize = 100;
      decimal totalPages = 1;
      decimal total = 0;
      try
      {
        do
        {
          UserDataFilterCriteria criteria = new UserDataFilterCriteria();
          criteria.PageSize = pageSize;
          criteria.CurrentPage = page;
          criteria.IsPagination = true;
          criteria.IsDormantedUsers = true;
          UserDataResponseInfo userDetails = await _wrapperUserService.GetUsersData(criteria);
          total = userDetails.RowCount;
          totalPages = userDetails.PageCount;
          page++;
          ArchiveUsers(userDetails);

        } while (page <= totalPages);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting user archival list, exception message =  {ex.Message}");
      }
    }

    public async void ArchiveUsers(UserDataResponseInfo userDetails)
    {
      if (userDetails != null)
      {
        if (!userDetails.UserList.Any())
        {
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          _logger.LogInformation("No users found for user achiving.");
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          return;
        }

        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation($"User archival list count: {userDetails.UserList.Count()}");
        _logger.LogInformation($"User archival - Test Mode: {_appSettings.TestModeSettings.Enable}");
        _logger.LogInformation($"User achival - Test Keyword: {_appSettings.TestModeSettings.Keyword}");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        foreach (var user in userDetails.UserList)
        {
          try
          {
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            _logger.LogInformation($"User Dormanted By: {user.DormantBy}, User Dormanted on UTC: {user.DormantedOnUtc}, User IsDormanted: {user.IsDormant}");
            DateTime currentDate = _dateTimeService.GetUTCNow();
            DateTime dt = currentDate.AddMinutes(-(user.DormantBy == DormantBy.Manual ? _appSettings.DormancyJobSettings.AdminDormantedUserArchivalDurationInMinutes : _appSettings.DormancyJobSettings.JobDormantedUserArchivalDurationInMinutes));
            _logger.LogInformation($"User dormanted date should be less than : {dt}");
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            if (user.IsDormant && user.DormantedOnUtc < dt)
            {
              _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
              _logger.LogInformation($"User to be archived: {user.UserName}");
              _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

              if (_appSettings.TestModeSettings.Enable)
              {
                if (user.UserName.Contains(_appSettings.TestModeSettings.Keyword))
                {
                  await _wrapperUserService.DeleteUserAsync(user.UserName, true);
                }
              }
              else
              {
                await _wrapperUserService.DeleteUserAsync(user.UserName, true);
              }              
            }
            else
            {
              _logger.LogInformation($"User {user.UserName} is not eligible for archival");
            }
          }
          catch (Exception ex)
          {
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            _logger.LogInformation($"User archival failed for the user: {user.UserName}");
            _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          }
        }
      }
    }
  }
}

