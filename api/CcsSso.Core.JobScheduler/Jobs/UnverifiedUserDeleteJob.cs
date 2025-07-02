using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CcsSso.Core.JobScheduler
{
  public class UnverifiedUserDeleteJob : BackgroundService
  {
    private IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly AppSettings _appSettings;
    private readonly IEmailSupportService _emailSupportService;    
    private readonly IIdamSupportService _idamSupportService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    public UnverifiedUserDeleteJob(IServiceProvider serviceProvider, IDateTimeService dataTimeService,
      AppSettings appSettings, IEmailSupportService emailSupportService, IIdamSupportService idamSupportService, 
      IHttpClientFactory httpClientFactory, IWrapperUserService wrapperUserService, IWrapperOrganisationService wrapperOrganisationService)
    {
      _dataTimeService = dataTimeService;
      _appSettings = appSettings;
      _emailSupportService = emailSupportService;
      _idamSupportService = idamSupportService;
      _serviceProvider = serviceProvider;
      _httpClientFactory = httpClientFactory;
      _wrapperUserService = wrapperUserService;
      _wrapperOrganisationService = wrapperOrganisationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
      {
        while (!stoppingToken.IsCancellationRequested)
        {
          Console.WriteLine($" **************** Unverified User Deletion job started ***********");
          _dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();          
          await PerformJobAsync();
          await Task.Delay(_appSettings.ScheduleJobSettings.UnverifiedUserDeletionJobExecutionFrequencyInMinutes * 60000, stoppingToken);
          Console.WriteLine($"****************** Unverified User Deletion job ended ***********");
        }
      }
    }

    public void InitiateScopedServices(IDataContext dataContext)
    {
      _dataContext = dataContext;      
    }

    public async Task PerformJobAsync()
    {
      Dictionary<string, List<string>> adminList = new Dictionary<string, List<string>>();

      var minimumThreshold = _appSettings.UserDeleteJobSettings.Min(udj => udj.UserDeleteThresholdInMinutes);
      var createdOnUtc = DateTime.UtcNow.AddMinutes(-1 * minimumThreshold);

      Console.WriteLine($"Retrieving unverified users created before {createdOnUtc}.");
      var inactiveUsers = await _wrapperUserService.GetInActiveUsers(createdOnUtc.ToString("yyyy-MM-dd HH:mm:ss"));

      if (inactiveUsers != null && inactiveUsers.Any())
        Console.WriteLine($"{inactiveUsers.Count()} user(s) found");
      else
        Console.WriteLine("No users found");


      var orgAdminList = new List<string>();
      foreach (var orgByUsers in inactiveUsers.GroupBy(u => u.OrganisationId))
      {
        Console.WriteLine($"Unverified User Deletion Organisation: {orgByUsers.Key}");
        foreach (var user in orgByUsers.Select(ou => ou).ToList())
        {
          Console.WriteLine($"Unverified User Deletion User: {user.UserName}");
          try
          {
            await _wrapperUserService.DeleteUserAsync(user.UserName);

            //if (user.UserAccessRolePendings.Any())
            //{
            //  var userPendingRoles = user.UserAccessRolePendings.Where(x => x.User.UserName == user.UserName).ToList();
            //  var roleIds = userPendingRoles.Select(x => x.OrganisationEligibleRoleId).ToList();
            //  await _wrapperUserService.RemoveApprovalPendingRoles(user.UserName, roleIds, DbModel.Constants.UserPendingRoleStaus.Expired);
            //  Console.WriteLine($" **************** Unverified User pending role deletion success for user:{user.UserName} **************** ");
            //}

            Console.WriteLine($" **************** Retrieving admins for org :{user.OrganisationId} **************** ");
            var filter = new UserFilterCriteria
            {
              isAdmin = true,
              includeSelf = true,
              includeUnverifiedAdmin = false,
              isDelegatedExpiredOnly = false,
              isDelegatedOnly = false,
              searchString = String.Empty,
              excludeInactive=true
            };

            var userDeleteJobSetting = _appSettings.UserDeleteJobSettings.FirstOrDefault(ud => ud.ServiceClientId == (user.ServiceClientId ?? ""));

            if (userDeleteJobSetting == null)
            {
              userDeleteJobSetting = _appSettings.UserDeleteJobSettings.FirstOrDefault(ud => ud.ServiceClientId == "ANY");
            }

            if (userDeleteJobSetting.NotifyOrgAdmin)
            {
              if (!adminList.Any(x => x.Key == user.OrganisationId))
              {
                var orgUsers = await _wrapperUserService.GetUserByOrganisation(user.OrganisationId, filter);
                adminList.Add(orgByUsers.Key, orgUsers.UserList.Select(au => au.UserName).ToList());
                Console.WriteLine($" **************** {orgUsers.UserList.Count()} admins found for the org:{user.OrganisationId} **************** ");
              }

              await _emailSupportService.SendUnVerifiedUserDeletionEmailToAdminAsync($"{user.FirstName} {user.LastName}", user.UserName, adminList[orgByUsers.Key]);
              Console.WriteLine($"Unverified User Notify Admin Success for: {user.UserName}");
            }

          }
          catch (Exception ex)
          {
            Console.WriteLine($"Error UnverifiedUserDeleteJob: {JsonConvert.SerializeObject(ex)}");
          }
        }
      }
    }
  }
}
