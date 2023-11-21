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

    public UserDeactivationService(IDateTimeService dateTimeService, ILogger<IUserDeactivationService> logger, IWrapperUserService wrapperUserService,
      IHttpClientFactory httpClientFactory, DormancyAppSettings dormancyAppSettings)

    {
      _dateTimeService = dateTimeService;
      _wrapperUserService = wrapperUserService;
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _appSettings = dormancyAppSettings;
    }

    #region UserDeactivation job
    public async Task PerformUserDeactivationJobAsync()
    {
      var userDeactivationList = await GetUserDeactivationList();

      if (!userDeactivationList.Any())
      {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        _logger.LogInformation("No users found for Deactivation.");
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        return;
      }

      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      _logger.LogInformation($"User Deactivation list count: {userDeactivationList.Count()}");
      _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      foreach (var user in userDeactivationList)
      {
        try
        {
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          _logger.LogInformation($"User to be dormanted: {user}");
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          await _wrapperUserService.DeactivateUserAsync(user, CcsSso.Domain.Constants.DormantBy.Job);
        }
        catch (Exception ex)
        {
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
          _logger.LogInformation($"User Deactivation failed for the user: {user}");
          _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }
      }
    }
    private async Task<List<string>> GetUserDeactivationList()
    {
      var userDeactivationList = new List<string>();
      try
      {
        DateTime currentDate = _dateTimeService.GetUTCNow();
        // Calculate the date 12 months ago
        string deactivationDuration = currentDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToString("yyyy-MM-dd");
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);
        var url = "security/data/users?date=" + HttpUtility.UrlEncode(deactivationDuration) + "&is-exact=false";
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          userDeactivationList = JsonConvert.DeserializeObject<List<string>>(content);
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
          throw new ResourceNotFoundException();
        }
        else
        {
          throw new CcsSsoException("ERROR_RETRIEVING_USER_LOGIN_DETAILS");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting user deactivation list, exception message =  {ex.Message}");
      }
      return userDeactivationList;
    }
    #endregion

  }
}
