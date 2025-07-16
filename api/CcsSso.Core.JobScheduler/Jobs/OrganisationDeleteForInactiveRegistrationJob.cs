using Amazon.Runtime;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Enum;
using CcsSso.Domain.Contracts;
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
  public class OrganisationDeleteForInactiveRegistrationJob : BackgroundService
  {
    private readonly AppSettings _appSettings;
    private readonly ICacheInvalidateService _cacheInvalidateService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperContactService _wrapperContactService;
    private readonly IHttpClientFactory _httpClientFactory;
    public OrganisationDeleteForInactiveRegistrationJob(AppSettings appSettings, 
      ICacheInvalidateService cacheInvalidateService,
      IWrapperOrganisationService wrapperOrganisationService,
      IWrapperUserService wrapperUserService,
      IWrapperContactService wrapperContactService,
      IHttpClientFactory httpClientFactory)
    {
      _appSettings = appSettings;
      _cacheInvalidateService = cacheInvalidateService;
      _wrapperOrganisationService = wrapperOrganisationService;
      _wrapperUserService = wrapperUserService;
      _wrapperContactService = wrapperContactService;
      _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

      while (!stoppingToken.IsCancellationRequested)
      {
        Console.WriteLine($" ****************Organization delete for inactive registration batch processing job started ***********");
        await PerformJobAsync();
        await Task.Delay(_appSettings.ScheduleJobSettings.InactiveOrganisationDeletionJobExecutionFrequencyInMinutes * 60000, stoppingToken);
        Console.WriteLine($"******************Organization delete for inactive registration batch processing job ended ***********");
      }
    }

    private async Task PerformJobAsync()
    {
      var organisationIds = await GetInactiveOrganisationAsync();
      Guid groupId = Guid.NewGuid();

      if (organisationIds != null)
        Console.WriteLine($"{organisationIds.Count()} organizations found");
      else
        Console.WriteLine("No organizations found");

      if (organisationIds != null)
      {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);
        client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);

        int i = 0;
        foreach (var orgDetail in organisationIds)
        {
          i++;
          Console.WriteLine($"Processing {i} out of {organisationIds.Count} Organisations");

          try
          {
            OrgDeleteCandidateStatus orgDeleteCandidateStatus = OrgDeleteCandidateStatus.None;

            string activeUserName = string.Empty;
            var adminUsers = await GetOrganisationAdmins(orgDetail.OrganisationId);

            Console.WriteLine($"{adminUsers.Count()} org admin(s) found in Org id {orgDetail.OrganisationId}");

            foreach (var adminUser in adminUsers)
            {
              var url = "/security/users?email=" + HttpUtility.UrlEncode(adminUser.UserName);
              var response = await client.GetAsync(url);
              var responseContent = await response.Content.ReadAsStringAsync();
              var idamUser = JsonConvert.DeserializeObject<IdamUser>(responseContent);

              if (idamUser != null)
              {

                if (idamUser.EmailVerified)
                {
                  orgDeleteCandidateStatus = OrgDeleteCandidateStatus.Activate;
                  activeUserName = adminUser.UserName;
                  break;
                }
                else
                {
                  orgDeleteCandidateStatus = OrgDeleteCandidateStatus.Delete;
                }
              }
              else
              {
                Console.WriteLine("The user doesn't exist in Auth0. But exists in our DB. So no action has been taken.");
                orgDeleteCandidateStatus = OrgDeleteCandidateStatus.None;
              }
            }

            if (orgDeleteCandidateStatus == OrgDeleteCandidateStatus.Delete)
            {
              await CreateOrgAuditLog(groupId, orgDetail);
              await DeleteOrganisationAsync(orgDetail.OrganisationId);
            }
            else if (orgDeleteCandidateStatus == OrgDeleteCandidateStatus.Activate)
            {
              Console.WriteLine($"*********Activating CII Organization id {orgDetail.OrganisationId} by user: {activeUserName} ***********************");
              await ActivateOrganisationAsync(activeUserName);
              Console.WriteLine($"*********Activated CII Organization id {orgDetail.OrganisationId} by user: {activeUserName} ***********************");
            }
          }
          catch (Exception e)
          {
            Console.WriteLine($"Failed to processed OrganisationId: {orgDetail.OrganisationId} from the list of Organisations");

            Console.WriteLine($"********* Org deletion error " + JsonConvert.SerializeObject(e));
          }
        }
      }
    }

    private async Task CreateOrgAuditLog(Guid groupId, InactiveOrganisationResponse orgDetail)
    {
      Console.WriteLine($"********* checking supplier buyer type NOT to be 0 for Org: {orgDetail.OrganisationId} ***********************");

      if (_appSettings.OrgAutoValidationJobSettings.Enable && orgDetail.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier)
      {
        var orgStatus = new WrapperOrganisationAuditInfo
        {
          Status = OrgAutoValidationStatus.AutoOrgRemoval,
          OrganisationId = orgDetail.OrganisationId,
          Actioned = OrganisationAuditActionType.Job.ToString(),
          ActionedBy = OrganisationAuditActionType.Job.ToString()
        };
        Console.WriteLine($"********* Start Update Organisation Audit List for Org: {orgDetail.OrganisationId} ***********************");
        var organisationAudit = await _wrapperOrganisationService.UpdateOrganisationAuditList(orgStatus);
        Console.WriteLine($"********* Finished Update Organisation Audit List for Org: {orgDetail.OrganisationId} ***********************");

        var eventLogs = new List<WrapperOrganisationAuditEventInfo>() {
                    new WrapperOrganisationAuditEventInfo
                    {
                      Actioned = OrganisationAuditActionType.Job.ToString(),
                      Event = OrganisationAuditEventType.InactiveOrganisationRemoved.ToString(),
                      GroupId = groupId,
                      OrganisationId = orgDetail.OrganisationId
                    }
                  };

        Console.WriteLine($"********* Start Create Organisation Audit Event Async List for Org: {orgDetail.OrganisationId} ***********************");
        await _wrapperOrganisationService.CreateOrganisationAuditEventAsync(eventLogs);
        Console.WriteLine($"********* Finished Create Organisation Audit Event Async List for Org: {orgDetail.OrganisationId} ***********************");
      }
    }

    public async Task ActivateOrganisationAsync(string userName)
    {
      await _wrapperOrganisationService.ActivateOrganisationByUser(userName);
    }

    public async Task DeleteOrganisationAsync(string ciiOrganisationId)
    {

      // Deleting Organisation Contact points

       await DeleteOrgUsers(ciiOrganisationId);
       await DeleteOrgContacts(ciiOrganisationId);

      await Task.Delay(1000);

      Console.WriteLine($"*********Start Deleting from Conclave Organization id {ciiOrganisationId}***********************");

      //if (deleteContactSuccess && deleteUsersSuccess)
      //{
        await _wrapperOrganisationService.DeleteOrganisationAsync(ciiOrganisationId);

        await DeleteCIIOrganisationEntryAsync(ciiOrganisationId);
      //}
      Console.WriteLine($"*********End Deleted from Conclave Organization id {ciiOrganisationId}***********************");

    }

    public async Task<List<InactiveOrganisationResponse>> GetInactiveOrganisationAsync()
    {
      var createdOnUtc = DateTime.UtcNow.AddMinutes(-1 * _appSettings.ScheduleJobSettings.OrganizationRegistrationExpiredThresholdInMinutes);
      var organisationIds = await _wrapperOrganisationService.GetInactiveOrganisationAsync(createdOnUtc.ToString("yyyy-MM-dd HH:mm:ss"));
      return organisationIds;
    }

    public async Task DeleteCIIOrganisationEntryAsync(string ciiOrganisationId)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("x-api-key", _appSettings.CiiSettings.Token);
      client.BaseAddress = new Uri(_appSettings.CiiSettings.Url);
      var url = "identities/organisations/" + ciiOrganisationId;
      var result = await client.DeleteAsync(url);
      if (result.IsSuccessStatusCode)
      {
        Console.WriteLine($"*********Deleted from CII Organization id {ciiOrganisationId} ***********************");
      }
      else
      {
        Console.WriteLine($"*********Could not delete from CII Organization id {ciiOrganisationId} ***********************");
      }
    }

    private async Task<List<UserListInfo>> GetOrganisationAdmins(string CiiOrganisationId)
    {

      var filter = new UserFilterCriteria
      {
        isAdmin = true,
        includeSelf = true,
        includeUnverifiedAdmin = true,
        isDelegatedExpiredOnly = false,
        isDelegatedOnly = false,
        searchString = String.Empty
      };

      var orgAdmins = await _wrapperUserService.GetUserByOrganisation(CiiOrganisationId, filter);
      return orgAdmins?.UserList;
    }

    private async Task DeleteOrgUsers(string ciiOrgId)
    {
      try
      {
        Console.WriteLine($"********* Start Deleting org users ***********************");

        var filter = new UserFilterCriteria
        {
          isAdmin = false,
          includeSelf = true,
          includeUnverifiedAdmin = true,
          isDelegatedExpiredOnly = false,
          isDelegatedOnly = false,
          searchString = String.Empty
        };

        var orgUsers = await _wrapperUserService.GetUserByOrganisation(ciiOrgId, filter);
        Console.WriteLine($"********* Total org user found: {orgUsers?.UserList.Count()} ***********************");

        List<Task> usersToDelete = new();
        List<Task> usersDeleteCache = new();
        var usersLists = orgUsers.UserList;
        if (usersLists.Any())
        {
          foreach (var user in usersLists)
          {
            await _wrapperUserService.DeleteAdminUserAsync(user.UserName);
            await _cacheInvalidateService.RemoveUserCacheValuesOnDeleteAsync(user.UserName, ciiOrgId, new List<int>());
          }
        }
       
        Console.WriteLine($"********* Deleting org users successful. ***********************");
      }
      catch(Exception ex)
      {
        Console.WriteLine($"********* Deleting org users fail Message{ex.Message}. ***********************");
      }
      //return deleteUserSuccess.IsCompletedSuccessfully;
    }

    private async Task DeleteOrgContacts(string ciiOrgId)
    {
      try
      {
        Console.WriteLine($"********* Start Deleting org contacts ***********************");
        var contactDetails = await _wrapperContactService.GetOrganisationContactListAsync(ciiOrgId);

        if (contactDetails != null && contactDetails.ContactPoints.Any())
        {
          Console.WriteLine($"********* Contacts found {contactDetails.ContactPoints.Count()} ***********************");
          var contacts = contactDetails.ContactPoints.OrderByDescending(item => item.ContactPointId).ToList();
          if (contacts.Any())
          {
            foreach (var contact in contacts)
            {
              
              if(contact.ContactPointReason != "REGISTRY")
              {
                await _wrapperContactService.DeleteOrganisationContactAsync(ciiOrgId, contact.ContactPointId);
              }
              else
              {
                await _wrapperContactService.DeleteOrganisationRegistryContactAsync(ciiOrgId);
              }
            }
          }

          var orgContactPointIds = contactDetails.ContactPoints.Select(cp => cp.ContactPointId).ToList<int>();
          await _cacheInvalidateService.RemoveOrganisationCacheValuesOnDeleteAsync(ciiOrgId, orgContactPointIds, new Dictionary<string, List<int>>());

          Console.WriteLine($"********* Deleting org contacts successful. ***********************");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"********* Deleting org contacts failed Message{ex.Message}. ***********************");
      }
    }
  }
}
