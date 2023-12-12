using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Microsoft.Extensions.Logging;
using CcsSso.Core.DormancyJobScheduler.Enum;

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
    private int totalNumberOfUsers = 0;
    DateTime cDate = DateTime.UtcNow;
    DateTime fDate = DateTime.UtcNow;
    DateTime tDate = DateTime.UtcNow;

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

    public async Task PerformDormancyResetNotificationJobAsync()
    {
      cDate = _dateTimeService.GetUTCNow();
      string currentDate = cDate.ToString("yyyy-MM-dd");

      _logger.LogInformation($"Test Mode: {_appSettings.TestModeSettings.Enable}");
      _logger.LogInformation($"Test Keyword: {_appSettings.TestModeSettings.Keyword}");
      _logger.LogInformation($"Current Date: {cDate}");

      string q = $"last_login:{currentDate} AND user_metadata.is_dormant_notified: true";

      await ProcessUserNotificationReset(q);

    }

    private async Task ProcessUserNotificationReset(string query)
    {
      _logger.LogInformation("*******************************************************************************************");
      _logger.LogInformation($"Notification Rest Job Query: {query}");

      try
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
          await ResetNotifyUsers(userDetails);

        } while (page <= totalPages);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting user notification reset list, exception message =  {ex.Message}");
      }
    }

    private async Task ResetNotifyUsers(UserDataList userDetails)
    {
      if (userDetails != null)
      {
        if (!userDetails.Users.Any())
        {
          _logger.LogInformation("No users found for notification reset.");
          return;
        }

        _logger.LogInformation($"User notification reset list count: {userDetails.Users.Count()}");

        foreach (var user in userDetails.Users)
        {
          try
          {
            totalNumberOfUsers = totalNumberOfUsers + 1;
            _logger.LogInformation($"User to be notify reset: {user.Email}");

            if (_appSettings.TestModeSettings.Enable)
            {
              if (user.Email.Contains(_appSettings.TestModeSettings.Keyword))
              {
                await _auth0Service.UpdateUserStatusAsync(user.Email, (int)UserStatusType.ResetNotify);
              }
            }
            else
            {
              await _auth0Service.UpdateUserStatusAsync(user.Email, (int)UserStatusType.ResetNotify);
            }
          }
          catch (Exception ex)
          {
            _logger.LogInformation($"User notification reset failed for the user: {user.Email}");
          }
        }
      }
    }

    public async Task PerformDormancyNotificationJobAsync()
    {
      cDate = _dateTimeService.GetUTCNow();
      string fromDate = string.Empty;
      string toDate = string.Empty;
      int notifyDuration = _appSettings.DormancyJobSettings.DeactivationNotificationInMinutes;

      if (_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes < 1440)
      {
        fDate = cDate.AddMinutes(-notifyDuration)
          .AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).ToUniversalTime();
        tDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(notifyDuration).ToUniversalTime();

        fromDate = fDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        toDate = tDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
      }
      else
      {
        fDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(notifyDuration);
        tDate = cDate.AddMinutes(-_appSettings.DormancyJobSettings.UserDeactivationDurationInMinutes).AddMinutes(notifyDuration);

        fromDate = fDate.ToString("yyyy-MM-dd");
        toDate = tDate.ToString("yyyy-MM-dd");
      }

      totalNumberOfUsers = 0;

      _logger.LogInformation($"Notification - Test Mode: {_appSettings.TestModeSettings.Enable}");
      _logger.LogInformation($"Notification - Test Keyword: {_appSettings.TestModeSettings.Keyword}");
      _logger.LogInformation($"Notification - Current Date: {cDate}");
      _logger.LogInformation($"Notification - From Date: {fDate}");
      _logger.LogInformation($"Notification - To Date: {tDate}");

      //if (_appSettings.TestModeSettings.Enable)
      //{
      //  string queryNormalDeactivateCase = $"last_login:[{fromDate} TO {toDate}] AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      //  await ProcessUserNotification(queryNormalDeactivateCase, false, true, "LAST_LOGIN");

      //  string queryNotLoginCase = $"created_at:[{fromDate} TO {toDate}] AND NOT _exists_:last_login AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      //  await ProcessUserNotification(queryNotLoginCase, false, true, "CREATED_AT");
      //}
      //else
      //{
      //string queryNormalDeactivateCase = $"last_login:[* TO {toDate}] AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      //await ProcessUserNotification(queryNormalDeactivateCase, false, false, "LAST_LOGIN");

      //string queryNotLoginCase = $"created_at:[* TO {toDate}] AND NOT _exists_:last_login AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      //await ProcessUserNotification(queryNotLoginCase, false, false, "CREATED_AT");
      //}

      string queryNormalDeactivateCase = $"last_login:{toDate} AND (user_metadata.is_re_activated:false OR NOT _exists_:user_metadata.is_re_activated) AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      await ProcessUserNotification(queryNormalDeactivateCase, false, false, "LAST_LOGIN");

      string queryNotLoginCase = $"created_at:{toDate} AND (user_metadata.is_re_activated:false OR NOT _exists_:user_metadata.is_re_activated) AND NOT _exists_:last_login AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      await ProcessUserNotification(queryNotLoginCase, false, false, "CREATED_AT");

      string queryNormalDeactivateWithReactiveCase = $"last_login:[* TO {toDate}] AND user_metadata.is_re_activated:true AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      await ProcessUserNotification(queryNormalDeactivateWithReactiveCase, false, true, "LAST_LOGIN");

      string queryNotLoginWithReactiveCase = $"created_at:[* TO {toDate}] AND user_metadata.is_re_activated:true AND NOT _exists_:last_login AND (user_metadata.is_dormant_notified: false OR NOT _exists_:user_metadata.is_dormant_notified)";
      await ProcessUserNotification(queryNotLoginWithReactiveCase, false, true, "CREATED_AT");

      _logger.LogInformation($"User Notification - Total Number Of Users: {totalNumberOfUsers}");

    }

    private async Task ProcessUserNotification(string query, bool isNext, bool isReactivation, string type)
    {
      _logger.LogInformation("*******************************************************************************************");
      _logger.LogInformation($"Notification Job Query: {query}");

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
            await NotifyUsers(userDetails, isReactivation, type);

          } while (page <= totalPages);

          hasNext = isNext && total > 0 ? true : false;

        } while (hasNext);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Error while getting user notification list, exception message =  {ex.Message}");
      }
    }

    private async Task NotifyUsers(UserDataList userDetails, bool isReactivation, string type)
    {
      if (userDetails != null)
      {
        if (!userDetails.Users.Any())
        {
          _logger.LogInformation("No users found for notification.");
          return;
        }

        _logger.LogInformation($"User notification list count: {userDetails.Users.Count()}");

        foreach (var user in userDetails.Users)
        {
          try
          {
            totalNumberOfUsers = totalNumberOfUsers + 1;
            _logger.LogInformation($"User to be notify: {user.Email}");

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
                  await NotifyUser(user);
                }
              }
            }
            else
            {
              await NotifyUser(user);
            }
          }
          catch (Exception ex)
          {
            _logger.LogInformation($"User Dormant Notification failed for the user: {user.Email}");
          }
        }
      }
    }

    private async Task NotifyUser(UserDataInfo user)
    {
      if (_appSettings.TestModeSettings.Enable)
      {
        if (user.Email.Contains(_appSettings.TestModeSettings.Keyword))
        {
          await SendNotification(user.Email);
        }
      }
      else
      {
        await SendNotification(user.Email);
      }
    }

    private async Task SendNotification(string email)
    {
      try
      {
        var userDetail = await _wrapperUserService.GetUserDetails(email);
        if (userDetail == null)
        {
          throw new Exception("Error fetching user Details" + email);
        }
        if (!userDetail.IsDormant)
        {
          var userEmailInfo = GetDormantNotificationEmailInfo(email);
          await _emaillProviderService.SendEmailAsync(userEmailInfo);
          await _auth0Service.UpdateUserStatusAsync(email, (int)UserStatusType.Notify);
        }
      }
      catch (Exception ex)
      {
        _logger.LogInformation($"User Dormant Notification failed: {ex.Message}");
      }
    }

    private EmailInfo GetDormantNotificationEmailInfo(string toEmailId)
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