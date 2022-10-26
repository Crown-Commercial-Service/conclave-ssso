using Amazon.Runtime;
using Amazon.S3.Model;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ServiceOnboardingScheduler.Model;
using CcsSso.DbModel.Entity;
using CcsSso.DbPersistence;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CcsSso.Core.ServiceOnboardingScheduler.Jobs
{
  public class CASOnboardingJob : BackgroundService
  {
    private readonly OnBoardingAppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;
    private readonly IDataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IEmailProviderService _emaillProviderService;

    private readonly ILogger<CASOnboardingJob> _logger;

    public CASOnboardingJob(ILogger<CASOnboardingJob> logger, OnBoardingAppSettings appSettings, IDateTimeService dataTimeService,
       IHttpClientFactory httpClientFactory, IServiceScopeFactory factory, IEmailProviderService emailProviderService)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _emaillProviderService = emailProviderService;

      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.CASOnboardingJobScheduleInMinutes * 60000;

        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        await PerformJob();

        await Task.Delay(interval, stoppingToken);
      }
    }

    private async Task PerformJob()
    {
      try
      {
        var listOfRegisteredOrgs = await GetRegisteredOrgsIds();


        if (listOfRegisteredOrgs == null || listOfRegisteredOrgs.Count() == 0)
        {
          _logger.LogInformation("No Organisation found");
          return;
        }
        var index = 0;

        foreach (var eachOrgs in listOfRegisteredOrgs)
        {
          index++;
          _logger.LogInformation($"trying to get adminDetail details of {index}");
          _logger.LogInformation($"OrgName {eachOrgs.Item3}");

          var adminList = await GetOrgAdmins(eachOrgs.Item1,eachOrgs.Item2);

          if (adminList == null)
          {
            _logger.LogWarning($"No Org admin found");
            continue;
          }
          _logger.LogInformation($"Org Admins {string.Join(", ", adminList.Select(t => $"['{t.Item1}', '{t.Item2}','{t.Item3}','{t.Item4}']"))}");


          var oldestOrgAdmin = adminList.OrderBy(x=>x.Item4).First();
          var domainName = oldestOrgAdmin.Item3.Substring(oldestOrgAdmin.Item3.Length - (oldestOrgAdmin.Item3.Length-oldestOrgAdmin.Item3.IndexOf('@')-1) );
          _logger.LogInformation($"Admin domain name  {domainName}");

          var isValid = await IsValidBuyer("brickendon.com");


          if (!isValid)
          {
            await AddDefaultRole(eachOrgs.Item1);

          }

          // now find the oldest admin

          //get the domain pass it to look up

          // passed then add role to organi 
          // add roles to all org admin Check before add new role.


          // FAiled 
          // send email with list of failed organisation


        }

        // var result = await IsValidBuyer("brickendon.com");

        var emailIds = new List<string>();
        emailIds.Add("local_test1@yopmail.com");
        var orgIds = new List<string>();
        orgIds.Add("test Org");

       // await SendFailedOrganizationEmailAsync(orgIds, emailIds);

       // _logger.LogInformation($"Autovalidation result {result}");


      }
      catch (Exception)
      {
        throw;
      }
    }

    private async Task AddDefaultRole(int orgId, List<string> adminList)
    {
      var roleList = new List<string>();
      roleList.Add("CAT_USER");
      roleList.Add("ACCESS_CAAAC_CLIENT");
      roleList.Add("JAGGAER_USER");
      roleList.Add("ACCESS_JAGGAER");

      var defaultRoles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted && roleList.Contains(ar.CcsAccessRoleNameKey)).ToListAsync();

      List<OrganisationEligibleRole> addedEligibleRoles = new List<OrganisationEligibleRole>();

      defaultRoles.ForEach(async (defaultRole) =>
      {
        var alreadyExist = await _dataContext.OrganisationEligibleRole.FirstOrDefaultAsync(x => x.OrganisationId == orgId && x.CcsAccessRoleId == defaultRole.Id);
        if (alreadyExist != null)
        {
          addedEligibleRoles.Add(new OrganisationEligibleRole
          {
            OrganisationId = orgId,
            CcsAccessRoleId = defaultRole.Id
          });
        }
      });

      if (addedEligibleRoles.Count > 0)
      {
        _dataContext.OrganisationEligibleRole.AddRange(addedEligibleRoles);
      }
      await _dataContext.SaveChangesAsync();




      foreach (var userName in adminList)
      {
        var user = await _dataContext.User
                  .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
                  .Include(u => u.UserAccessRoles)
                  .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);
        
        if (user == null)
        {
          _logger.LogInformation($"No user found in user table for user {userName}");
        }

        List<UserAccessRole> userAccessRoles = new List<UserAccessRole>();

        foreach (var role in addedEligibleRoles)
        {
          var organisationAdminAccessRole = await _dataContext.OrganisationEligibleRole
           .FirstOrDefaultAsync(oer => !oer.IsDeleted && oer.OrganisationId == user.Party.Person.OrganisationId && oer.CcsAccessRole.Id == role.CcsAccessRoleId && oer.OrganisationId==orgId);

          if(organisationAdminAccessRole == null)
          {
            _logger.LogInformation($"No organisation eligible access exists for organisation {orgId}");
            continue;
          }

          var alreadyExist = await _dataContext.UserAccessRole.FirstOrDefaultAsync(x => x.UserId == user.Id && x.OrganisationEligibleRoleId == organisationAdminAccessRole.Id);

          if (alreadyExist != null)
          {
            userAccessRoles.Add(new UserAccessRole
            {
              UserId = user.Id,
              OrganisationEligibleRoleId = organisationAdminAccessRole.Id
            });
          }
          else
          {
            _logger.LogInformation($"User role already exists. User id  {user.Id} and org eligible role id {organisationAdminAccessRole.id}");
          }
        }

        if (userAccessRoles.Count > 0)
        {
          _dataContext.UserAccessRole.AddRange(userAccessRoles);
        }

        await _dataContext.SaveChangesAsync();
      }

    }

    private async Task<List<Tuple<int, string, string,DateTime>>> GetOrgAdmins(int orgId, string ciiOrganisationId)
    {

      var orgAdminAccessRole = await _dataContext.OrganisationEligibleRole.FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == orgId && or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey);

      if (orgAdminAccessRole != null)
      {
        var users = await _dataContext.User
                .Include(u => u.Party).ThenInclude(p => p.Person)
                .Include(u => u.UserAccessRoles)
                .Where(u => !u.IsDeleted &&
                u.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId &&  //u.AccountVerified == true &&
                u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRole.Id))
                .OrderBy(u => u.Party.Person.FirstName).ThenBy(u => u.Party.Person.LastName)
                .Select(o => new Tuple<int, string, string, DateTime>(o.Id, o.Party.Person.FirstName + o.Party.Person.FirstName, o.UserName, o.CreatedOnUtc)).ToListAsync();


        return users;
      }

      return null;

    }

    private async Task<List<Tuple<int, string,string>>> GetRegisteredOrgsIds()
    {
      var dataDuration = _appSettings.OnBoardingDataDuration.CASOnboardingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsDeleted && org.RightToBuy==false && org.SupplierBuyerType>0 // ToDo: change to buyer or both
                          && org.LastUpdatedOnUtc > untilDateTime)
                          .Select(o => new Tuple<int, string,string>(o.Id, o.CiiOrganisationId,o.LegalName)).ToListAsync();
        return organisationIds;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }


    }

    private async Task SendFailedOrganizationEmailAsync(List<string> organizations, List<string> toEmails)
    {

      string orgNames = String.Join(", ", organizations.Select(x => x));


      var data = new Dictionary<string, dynamic>
      {
        //{ "link_to_file", NotificationClient.PrepareUpload(documentContents, true) }
        { "link_to_file", orgNames }
      };



      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in toEmails)
      {
        var emailInfo = GetEmailInfo(toEmail, "69e0933d-23af-4e6d-8fa5-e96f199a7492", data);



        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }



      await Task.WhenAll(emailTaskList);
    }

    private EmailInfo GetEmailInfo(string toEmail, string templateId, Dictionary<string, dynamic> data)
    {
      var emailInfo = new EmailInfo
      {
        To = toEmail,
        TemplateId = templateId,
        BodyContent = data
      };

      return emailInfo;
    }

    private async Task<bool> IsValidBuyer(string domain)
    {
      var client = _httpClientFactory.CreateClient("LookupApi");
      var url = "/lookup?domainname=" + domain; 
      var response = await client.GetAsync(url); 
      if (response.StatusCode == System.Net.HttpStatusCode.OK) 
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        var isValid = JsonConvert.DeserializeObject<bool>(responseContent); 
        return isValid; 
      }

      return false;
    }



  }

}
