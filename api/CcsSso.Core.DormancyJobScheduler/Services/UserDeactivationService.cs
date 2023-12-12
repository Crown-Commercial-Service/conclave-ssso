using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.Extensions.Logging;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.DormancyJobScheduler.Model;
using System.Linq;

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
    private int totalNumberOfUsers = 0;
    DateTime cDate = DateTime.UtcNow;
    DateTime fDate = DateTime.UtcNow;
    DateTime tDate = DateTime.UtcNow;

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
      cDate = _dateTimeService.GetUTCNow();
      string fromDate = string.Empty;
      string toDate = string.Empty;
      if (_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes < 1440)
      {

        fDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes)
          .AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToUniversalTime();
        tDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToUniversalTime();

        fromDate = fDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        toDate = tDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
      }
      else
      {
        //Calculate the date 12 months ago
        fDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes);
        tDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes);

        fromDate = fDate.ToString("yyyy-MM-dd");
        toDate = tDate.ToString("yyyy-MM-dd");
      }

      totalNumberOfUsers = 0;

      _logger.LogInformation($"Deactivation - Test Mode: {_appSettings.TestModeSettings.Enable}");
      _logger.LogInformation($"Deactivation - Test Keyword: {_appSettings.TestModeSettings.Keyword}");
      _logger.LogInformation($"Deactivation - Current Date: {cDate}");
      _logger.LogInformation($"Deactivation - From Date: {fDate}");
      _logger.LogInformation($"Deactivation - To Date: {tDate}");

      //if (_appSettings.TestModeSettings.Enable)
      //{
      //  string queryNormalDeactivateCase = $"last_login:[{fromDate} TO {toDate}] AND (user_metadata.is_deactivated: false OR NOT _exists_:user_metadata.is_deactivated)";
      //  await ProcessUserDeactivation(queryNormalDeactivateCase, false, true, "LAST_LOGIN");

      //  string queryNotLoginCase = $"created_at:[{fromDate} TO {toDate}] AND NOT _exists_:last_login AND (user_metadata.is_deactivated: false OR NOT _exists_:user_metadata.is_deactivated)";
      //  await ProcessUserDeactivation(queryNotLoginCase, false, true, "CREATED_AT");
      //}
      //else
      //{
      string queryNormalDeactivateCase = $"last_login:[* TO {toDate}] AND (user_metadata.is_deactivated: false OR NOT _exists_:user_metadata.is_deactivated)";
      await ProcessUserDeactivation(queryNormalDeactivateCase, false, true, "LAST_LOGIN");

      string queryNotLoginCase = $"created_at:[* TO {toDate}] AND NOT _exists_:last_login AND (user_metadata.is_deactivated: false OR NOT _exists_:user_metadata.is_deactivated)";
      await ProcessUserDeactivation(queryNotLoginCase, false, true, "CREATED_AT");
      //}

      _logger.LogInformation($"User Deactivation - Total Number Of Users: {totalNumberOfUsers}");
    }

    private async Task ProcessUserDeactivation(string query, bool isNext, bool isReactivation, string type)
    {
      _logger.LogInformation($"Deactivation Job Query: {query}");

      try
      {
        bool hasNext = false;

        do
        {
          int page = 1;
          int pageSize = 100;
          decimal totalPages = 1;
          decimal total = 0;

          do
          {
            UserDataList userDetails = await _auth0Service.GetUsersDataAsync(query, page - 1, pageSize);
            total = userDetails.Total;
            totalPages = Math.Ceiling(total / pageSize);
            page++;
            await DeactivateUsers(userDetails, isReactivation, type);

          } while (page <= totalPages);

          hasNext = isNext && total > 0 ? true : false;

        } while (hasNext);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting user deactivation list, exception message =  {ex.Message}");
      }
    }

    public async Task DeactivateUsers(UserDataList userDetails, bool isReactivation, string type)
    {
      if (userDetails != null)
      {
        if (!userDetails.Users.Any())
        {
          _logger.LogInformation("No users found for Deactivation.");
          return;
        }

        _logger.LogInformation($"User Deactivation list count: {userDetails.Users.Count()}");

        foreach (var user in userDetails.Users)
        {
          try
          {
            totalNumberOfUsers = totalNumberOfUsers + 1;
            _logger.LogInformation($"User to be dormanted: {user.Email}");

            if (isReactivation
              && user.UserMetadata?.IsReactivated == true && user.UserMetadata.ReactivatedOn != null)
            {
              _logger.LogInformation("User belongs to reactivation case:");

              DateTime reactivatedOn = user.UserMetadata.ReactivatedOn.Value;
              DateTime? lastActivityDate = null;
              if (type == "LAST_LOGIN" && user.LastLogin.HasValue)
              {
                lastActivityDate = user.LastLogin.Value;
              }
              else if (type == "CREATED_AT" && user.CreatedAt.HasValue)
              {
                lastActivityDate = user.CreatedAt.Value;
              }

              if (lastActivityDate != null)
              {
                if ((lastActivityDate < reactivatedOn && reactivatedOn < tDate)
                  || (lastActivityDate > reactivatedOn && lastActivityDate < tDate))
                {
                  await DeactivateUser(user);
                }
              }
            }
            else
            {
              await DeactivateUser(user);
            }
          }
          catch (Exception ex)
          {
            _logger.LogInformation($"User Deactivation failed for the user: {user.Email}");
          }
        }
      }
    }

    private async Task DeactivateUser(UserDataInfo user)
    {
      if (_appSettings.TestModeSettings.Enable)
      {
        if (user.Email.Contains(_appSettings.TestModeSettings.Keyword))
        {
          await _wrapperUserService.DeactivateUserAsync(user.Email, CcsSso.Domain.Constants.DormantBy.Job);
        }
      }
      else
      {
        await _wrapperUserService.DeactivateUserAsync(user.Email, CcsSso.Domain.Constants.DormantBy.Job);
      }
    }

    #endregion
  }
}
