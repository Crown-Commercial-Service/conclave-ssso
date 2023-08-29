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
    private IOrganisationSupportService _organisationSupportService;
    private readonly IIdamSupportService _idamSupportService;
    private readonly IServiceProvider _serviceProvider;
    private IContactSupportService _contactSupportService;
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
          _organisationSupportService = scope.ServiceProvider.GetRequiredService<IOrganisationSupportService>();
          _contactSupportService = scope.ServiceProvider.GetRequiredService<IContactSupportService>();
          await PerformJobAsync();
          await Task.Delay(_appSettings.ScheduleJobSettings.UnverifiedUserDeletionJobExecutionFrequencyInMinutes * 60000, stoppingToken);
          Console.WriteLine($"****************** Unverified User Deletion job ended ***********");
        }
      }
    }

    public void InitiateScopedServices(IDataContext dataContext, IOrganisationSupportService organisationSupportService)
    {
      _dataContext = dataContext;
      _organisationSupportService = organisationSupportService;
    }

    public async Task PerformJobAsync()
    {
      Dictionary<int, List<string>> adminList = new Dictionary<int, List<string>>();
      Dictionary<int, bool> orgSiteContactAvailabilityStatus = new Dictionary<int, bool>();

      var minimumThreshold = _appSettings.UserDeleteJobSettings.Min(udj => udj.UserDeleteThresholdInMinutes);
      var createdOnUtc = DateTime.UtcNow.AddMinutes(-1 * minimumThreshold);
      var users = await _wrapperUserService.GetInActiveUsers(createdOnUtc.ToString("dd-MM-yyyy HH:mm:ss"));

      if (users != null)
        Console.WriteLine($"{users.Count()} user(s) found");
      else
        Console.WriteLine("No users found");


      var orgAdminList = new List<string>();
      foreach (var orgByUsers in users.GroupBy(u => u.Id))
      {
        Console.WriteLine($"Unverified User Deletion Organisation: {orgByUsers.Key}");
        foreach (var user in orgByUsers.Select(ou => ou).ToList())
        {
          Console.WriteLine($"Unverified User Deletion User: {user.UserName} Id:{user.Id}");
          try
          {
            await _wrapperUserService.DeleteUserAsync(user.UserName);

            if (user.UserAccessRolePendings.Any())
            {
              var userPendingRoles = user.UserAccessRolePendings.Where(x => x.UserId == user.Id).ToList();
              var roleIds = userPendingRoles.Select(x => x.OrganisationEligibleRoleId).ToList();
              await _wrapperUserService.RemoveApprovalPendingRoles(user.UserName, roleIds, DbModel.Constants.UserPendingRoleStaus.Expired);
              Console.WriteLine($" **************** Unverified User pending role deletion success for user:{user.UserName} **************** ");
            }


            var filter = new UserFilterCriteria
            {
              isAdmin = true,
              includeSelf = true,
              includeUnverifiedAdmin = false,
              isDelegatedExpiredOnly = false,
              isDelegatedOnly = false,
              searchString = String.Empty
            };
            var adminUsers = await _wrapperUserService.GetUserByOrganisation(orgByUsers.Key, filter);
            adminList.Add(orgByUsers.Key, adminUsers.Where(au => au.AccountVerified).Select(au => au.UserName).ToList());

            await _emailSupportService.SendUnVerifiedUserDeletionEmailToAdminAsync($"{user.FirstName} {user.LastName}", user.UserName, adminList[orgByUsers.Key]);
            Console.WriteLine($"Unverified User Notify Admin Success for: {user.UserName}");
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
