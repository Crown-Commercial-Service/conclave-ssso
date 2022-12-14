using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.ServiceOnboardingScheduler.Model;
using CcsSso.Core.ServiceOnboardingScheduler.Service;
using CcsSso.Core.ServiceOnboardingScheduler.Service.Contracts;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Notify.Client;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Organisation = CcsSso.Core.ServiceOnboardingScheduler.Model.Organisation;

namespace CcsSso.Core.ServiceOnboardingScheduler.Jobs
{
  public class OneTimeCASOnboardingJob : BackgroundService
  {
    private readonly OnBoardingAppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;
    private readonly IDataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailProviderService _emaillProviderService;
    private readonly ICASOnBoardingService _onBoardingService;
    private readonly ILogger<CASOnboardingJob> _logger;
    private bool ranOnce;
    private bool reportingMode;
    private DateTime startDate;
    private DateTime endDate;


    public OneTimeCASOnboardingJob(ILogger<CASOnboardingJob> logger, OnBoardingAppSettings appSettings, IDateTimeService dataTimeService,
       IHttpClientFactory httpClientFactory, IServiceScopeFactory factory, IEmailProviderService emailProviderService,
       ICASOnBoardingService onBoardingService)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _emaillProviderService = emailProviderService;
      _onBoardingService = onBoardingService;
      ranOnce = false;

      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {

        int interval = _appSettings.ScheduleJobSettings.CASOnboardingJobScheduleInMinutes * 60000;
        reportingMode = _appSettings.ReportingMode;

        var oneTimeValidationSwitch = _appSettings.OneTimeValidation.Switch;

        if (oneTimeValidationSwitch && ranOnce)
        {
          _logger.LogInformation("One time validation ran already. Skipping this iteration.");
          await Task.Delay(interval, stoppingToken);
          continue;
        }
        if (oneTimeValidationSwitch)
        {
          var startDateString = _appSettings.OneTimeValidation.StartDate;
          var endDateString = _appSettings.OneTimeValidation.EndDate;

          if (startDateString == null || endDateString == null)
          {
            _logger.LogError("One time validation needs start and end date. Skipping this iteration.");
            await Task.Delay(interval, stoppingToken);
            continue;

          }
          try
          {
            startDate = DateTime.ParseExact(startDateString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            endDate = DateTime.ParseExact(endDateString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

          }
          catch (FormatException)
          {
            _logger.LogError("{0} or {1} is not in the correct format. Date format should be as follows 'yyyy-MM-dd' Skipping this iteration.", startDateString, endDateString);
            await Task.Delay(interval, stoppingToken);
            continue;
          }
          catch (Exception)
          {
            _logger.LogError("Error while reading the start or end date {0}, {1}. Skipping this iteration.", startDateString, endDateString);
            await Task.Delay(interval, stoppingToken);
            continue;
          }


          _logger.LogInformation("One time validation job switched on. So it runs once to process all the organisation between dates");
        }

        _logger.LogInformation("");
        _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        await PerformJob(oneTimeValidationSwitch);
        ranOnce = true;

        _logger.LogInformation("Worker Finsied at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");

        await Task.Delay(interval, stoppingToken);
      }
    }

    private async Task PerformJob(bool oneTimeValidationSwitch)
    {
      try
      {
        //var successOrgList = new List<string>();
        //var failedOrgList = new List<string>();

        //var jobReport = new List<string>();
        var jobReport = new List<LogJobDetail>();

        var listOfRegisteredOrgs = await _onBoardingService.GetRegisteredOrgsIds(oneTimeValidationSwitch, startDate, endDate);

        List<OrganizationDetails> failedOrganizations = new List<OrganizationDetails>();

        var emailIds = _appSettings.EmailSettings.EmailIds;
        if (emailIds == null)
        {
          _logger.LogWarning($"No sender email id found. ");
        }

        var toEmailIds = _appSettings.EmailSettings.EmailIds.ToList();

        var logEmailIds = _appSettings.LogReportEmailId;

        if (listOfRegisteredOrgs == null || listOfRegisteredOrgs.Count() == 0)
        {
          _logger.LogInformation("No Organisation found");
          return;
        }

        _logger.LogInformation($"Number of organisation {listOfRegisteredOrgs.Count()}");

        foreach (var eachOrgs in listOfRegisteredOrgs)
        {
          var orgId = eachOrgs.Id;
          var ciiOrgId = eachOrgs.CiiOrganisationId;
          var orgLegalName = eachOrgs.LegalName;
          var orgCreatedOn = eachOrgs.CreatedOnUtc;

          try
          {
            _logger.LogInformation($"OrgName {orgLegalName}");

            var adminList = await _onBoardingService.GetOrgAdmins(orgId, ciiOrgId);

            if (adminList == null)
            {
              _logger.LogWarning($"No Org admin found");
              continue;
            }

            _logger.LogInformation($"Org Admins {string.Join(", ", adminList.Select(t => $"['{t.Item1}', '{t.Item2}','{t.Item3}','{t.Item4}']"))}");


            var oldestOrgAdmin = adminList.OrderBy(x => x.Item4).First();
            var domainName = oldestOrgAdmin.Item3.Substring(oldestOrgAdmin.Item3.Length - (oldestOrgAdmin.Item3.Length - oldestOrgAdmin.Item3.IndexOf('@') - 1));

            _logger.LogInformation($"Admin domain name  {domainName}");

            var isValid = false;

            if (eachOrgs.SupplierBuyerType > 0)
            {
              if (eachOrgs.RightToBuy == false)
                isValid = await IsValidBuyer(domainName);
              else
                isValid = true;
            }
            else
            {
              if (!reportingMode)
              {
                await AddSupplierRole(eachOrgs.Id, adminList.Select(t => t.Item3).ToList());
              }
              else
              {
                _logger.LogInformation($"Reporting Mode is On. So no default roles are assigned");
              }
            }

            jobReport.Add(new LogJobDetail()
            {
              Id = ciiOrgId,
              Name = orgLegalName,
              SupplierBuyerType = eachOrgs.SupplierBuyerType,
              AdminEmail = oldestOrgAdmin.Item3,
              AutovalidationStatus = isValid.ToString(),
              DateTime = ConvertToGmtDateTime(orgCreatedOn)
            });

            if (eachOrgs.SupplierBuyerType > 0)
            {
              if (!reportingMode)
              {
                if (isValid)
                {
                  _logger.LogInformation($"Auto validation succeeded for org. LegalName =  {eachOrgs.LegalName}");

                  if (eachOrgs.SupplierBuyerType > 0)
                  {
                    await UpdateRightToBuy(eachOrgs.Id);
                    await AddDefaultRole(eachOrgs.Id, adminList.Select(t => t.Item3).ToList());
                  }

                }
                else
                {

                  _logger.LogInformation($"Auto validation failed for org. LegalName =  {eachOrgs.CiiOrganisationId}");

                  OrganizationDetails organizationDetails = new OrganizationDetails();
                  organizationDetails.Id = eachOrgs.CiiOrganisationId.ToString();
                  organizationDetails.Name = eachOrgs.LegalName;
                  organizationDetails.AdminEmail = oldestOrgAdmin.Item3;
                  organizationDetails.AutovalidationStatus = "false";
                  organizationDetails.DateTime = ConvertToGmtDateTime(orgCreatedOn);

                  failedOrganizations.Add(organizationDetails);
                }
              }
              else
              {
                _logger.LogInformation($"Reporting Mode is On. So no default roles are assigned");
              }
            }
          }
          catch (Exception ex)
          {
            _logger.LogError($"*****Inner Exception while processing the org: {eachOrgs.CiiOrganisationId}, exception message =  {ex.Message}");
            //failedOrgList.Add($"OrgId:{orgId}-CII Org Id:{ciiOrgId}-Org LegalName:{orgLegalName}");
          }
        }

        if (jobReport.Count > 0)
        {
          _logger.LogInformation("***********************");
          _logger.LogInformation("Report");
          foreach (var eachOrgs in jobReport)
          {
            _logger.LogInformation($"CII Org Id:{eachOrgs.Id}-Org LegalName:{eachOrgs.Name}-Autovalidation:{eachOrgs.AutovalidationStatus}" +
              $"-oldest org admin:{eachOrgs.AdminEmail}--Org type:{eachOrgs.SupplierBuyerType}");
          }

          if (!string.IsNullOrEmpty(logEmailIds))
          {
            await SendLogDetailEmailAsync(jobReport, new List<string> { logEmailIds });
          }
        }

        if (failedOrganizations.Count > 0)
        {
          await SendFailedOrganizationEmailAsync(failedOrganizations, toEmailIds); //new List<string> { "" }
        }

      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Outer Exception during this schedule, exception message =  {ex.Message}");
      }
    }

    private async Task AddSupplierRole(int orgId, List<string> adminList)
    {
      var roleList = new List<string>();
      var roles = _appSettings.SupplierRoles;

      if (roles == null)
      {
        roleList.Add("JAGGAER_USER");
      }
      else
      {
        roleList = roles.ToList();
      }


      var supplierRoles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted && roleList.Contains(ar.CcsAccessRoleNameKey)).ToListAsync();

      List<OrganisationEligibleRole> addedEligibleRoles = new List<OrganisationEligibleRole>();

      var orgRoles = await _dataContext.OrganisationEligibleRole.Where(x => x.OrganisationId == orgId && !x.IsDeleted).ToListAsync();

      supplierRoles.ForEach(async (role) =>
      {
        var alreadyExist = orgRoles.FirstOrDefault(x => x.OrganisationId == orgId && x.CcsAccessRoleId == role.Id);
        if (alreadyExist == null)
        {
          addedEligibleRoles.Add(new OrganisationEligibleRole
          {
            OrganisationId = orgId,
            CcsAccessRoleId = role.Id
          });

          _logger.LogInformation($"Added org role:{role.CcsAccessRoleNameKey}");
        }
        else
        {
          _logger.LogInformation($"Org role {role.CcsAccessRoleNameKey} already exists. org Id  {orgId}");
        }
      });

      if (addedEligibleRoles.Count > 0)
      {
        _dataContext.OrganisationEligibleRole.AddRange(addedEligibleRoles);
        await _dataContext.SaveChangesAsync();
      }
    }

    private async Task UpdateRightToBuy(int orgId)
    {
      var org = await _dataContext.Organisation.FirstOrDefaultAsync(or => !or.IsDeleted && or.Id == orgId);
      org.RightToBuy = true;
      await _dataContext.SaveChangesAsync();
      _logger.LogInformation($"Right to buy flag updated for Org id:{org.Id}, Name: {org.LegalName}");

    }

    private async Task AddDefaultRole(int orgId, List<string> adminList)
    {
      var roleList = new List<string>();
      var roles = _appSettings.CASDefaultRoles;

      if (roles == null)
      {
        roleList.Add("CAT_USER");
        roleList.Add("ACCESS_CAAAC_CLIENT");
        roleList.Add("JAGGAER_USER");
        roleList.Add("JAEGGER_BUYER"); //As per the discussion with Artur - Change from ACCESS_JAGGAER to JAEGGER_BUYER
      }
      else
      {
        roleList = roles.ToList();
      }


      var defaultRoles = await _dataContext.CcsAccessRole.Where(ar => !ar.IsDeleted && roleList.Contains(ar.CcsAccessRoleNameKey)).ToListAsync();

      List<OrganisationEligibleRole> addedEligibleRoles = new List<OrganisationEligibleRole>();

      var orgRoles = await _dataContext.OrganisationEligibleRole.Where(x => x.OrganisationId == orgId && !x.IsDeleted).ToListAsync();

      defaultRoles.ForEach((defaultRole) =>
      {
        var alreadyExist = orgRoles.FirstOrDefault(x => x.OrganisationId == orgId && x.CcsAccessRoleId == defaultRole.Id);
        if (alreadyExist == null)
        {
          addedEligibleRoles.Add(new OrganisationEligibleRole
          {
            OrganisationId = orgId,
            CcsAccessRoleId = defaultRole.Id
          });

          _logger.LogInformation($"Added org role:{defaultRole.CcsAccessRoleNameKey}");
        }
        else
        {
          _logger.LogInformation($"Org role {defaultRole.CcsAccessRoleNameKey} already exists. org Id  {orgId}");
        }
      });

      if (addedEligibleRoles.Count > 0)
      {
        _dataContext.OrganisationEligibleRole.AddRange(addedEligibleRoles);
        await _dataContext.SaveChangesAsync();
      }



      foreach (var userName in adminList)
      {
        var user = await _dataContext.User
                  .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(p => p.Organisation)
                  .Include(u => u.UserAccessRoles)
                  .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName && u.UserType == UserType.Primary);

        if (user == null)
        {
          _logger.LogInformation($"No user found in user table for user {userName}");
          continue;
        }

        List<UserAccessRole> userAccessRoles = new List<UserAccessRole>();

        foreach (var role in defaultRoles)
        {
          var organisationAdminAccessRole = await _dataContext.OrganisationEligibleRole
           .FirstOrDefaultAsync(oer => !oer.IsDeleted && oer.OrganisationId == user.Party.Person.OrganisationId && oer.CcsAccessRole.CcsAccessRoleNameKey == role.CcsAccessRoleNameKey && oer.OrganisationId == orgId);

          if (organisationAdminAccessRole == null)
          {
            _logger.LogInformation($"No organisation eligible access role exists for organisation {orgId}");
            continue;
          }

          var alreadyExist = await _dataContext.UserAccessRole.FirstOrDefaultAsync(x => x.UserId == user.Id && x.OrganisationEligibleRoleId == organisationAdminAccessRole.Id && !x.IsDeleted);

          if (alreadyExist == null)
          {
            userAccessRoles.Add(new UserAccessRole
            {
              UserId = user.Id,
              OrganisationEligibleRoleId = organisationAdminAccessRole.Id
            });
            _logger.LogInformation($"Role added to OrgAdmin Role:{role.CcsAccessRoleNameKey}, org eligible role id {organisationAdminAccessRole.Id}, User email: {user.UserName}");
          }
          else
          {
            _logger.LogInformation($"User role already exists. User id  {user.UserName} and org eligible role id {organisationAdminAccessRole.Id}");
          }
        }

        if (userAccessRoles.Count > 0)
        {
          _dataContext.UserAccessRole.AddRange(userAccessRoles);
        }

        await _dataContext.SaveChangesAsync();
      }

    }

    private async Task SendLogDetailEmailAsync(List<LogJobDetail> logs, List<string> toEmails)
    {
      byte[] documentContents = ConvertToCsv(logs);


      var data = new Dictionary<string, dynamic>
      {
        { "link_to_file", NotificationClient.PrepareUpload(documentContents, true) }

      };

      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in toEmails)
      {
        var emailTempalteId = _appSettings.EmailSettings.FailedAutoValidationNotificationTemplateId;
        var emailInfo = GetEmailInfo(toEmail, emailTempalteId, data); //"69e0933d-23af-4e6d-8fa5-e96f199a7492"



        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }

      await Task.WhenAll(emailTaskList);
    }

    private async Task SendFailedOrganizationEmailAsync(List<OrganizationDetails> organizations, List<string> toEmails)
    {
      byte[] documentContents = ConvertToCsv(organizations);


      var data = new Dictionary<string, dynamic>
      {
        { "link_to_file", NotificationClient.PrepareUpload(documentContents, true) }

      };

      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in toEmails)
      {
        var emailTempalteId = _appSettings.EmailSettings.FailedAutoValidationNotificationTemplateId;
        var emailInfo = GetEmailInfo(toEmail, emailTempalteId, data); //"69e0933d-23af-4e6d-8fa5-e96f199a7492"



        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }



      await Task.WhenAll(emailTaskList);
    }
    private static byte[] ConvertToCsv(List<OrganizationDetails> organizations)
    {
      var csv = new StringBuilder();

      var csvHeader = string.Format("{0},{1},{2},{3},{4}", "Organisation Id", "Organisation Name", "Administrator Email", "Autovalidation Status", "Date and Time");
      csv.AppendLine(csvHeader);

      foreach (var item in organizations)
      {
        var newLine = string.Format("{0},{1},{2},{3},{4}", item.Id, item.Name, item.AdminEmail, item.AutovalidationStatus, item.DateTime);
        csv.AppendLine(newLine);
      }

      byte[] documentContents = Encoding.ASCII.GetBytes(csv.ToString());
      return documentContents;
    }

    private static byte[] ConvertToCsv(List<LogJobDetail> logs)
    {
      var csv = new StringBuilder();

      var csvHeader = string.Format("{0},{1},{2},{3},{4},{5}", "Organisation Id", "Organisation Name", "Administrator Email", "Autovalidation Status", "Date and Time", "Org Type");
      csv.AppendLine(csvHeader);

      foreach (var item in logs)
      {
        var newLine = string.Format("{0},{1},{2},{3},{4},{5}", item.Id, item.Name, item.AdminEmail, item.AutovalidationStatus, item.DateTime, item.SupplierBuyerType);
        csv.AppendLine(newLine);
      }

      byte[] documentContents = Encoding.ASCII.GetBytes(csv.ToString());
      return documentContents;
    }

    //private async Task SendFailedOrganizationEmailAsync(List<string> organizations, List<string> toEmails)
    //{

    //  string orgNames = String.Join(", ", organizations.Select(x => x));


    //  var data = new Dictionary<string, dynamic>
    //  {
    //    //{ "link_to_file", NotificationClient.PrepareUpload(documentContents, true) }
    //    { "link_to_file", orgNames }
    //  };



    //  List<Task> emailTaskList = new List<Task>();
    //  foreach (var toEmail in toEmails)
    //  {
    //    var emailInfo = GetEmailInfo(toEmail, "69e0933d-23af-4e6d-8fa5-e96f199a7492", data);



    //    emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
    //  }



    //  await Task.WhenAll(emailTaskList);
    //}

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

    private string ConvertToGmtDateTime(DateTime dateTime)
    {
      var easternZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
      var convertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, easternZone);
      return convertedDateTime.ToString("dd/MM/yyyy hh:mm", CultureInfo.InvariantCulture);
    }

  }
}
