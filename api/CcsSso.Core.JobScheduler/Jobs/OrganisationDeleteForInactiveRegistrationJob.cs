using Amazon.Runtime;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
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
    private readonly IHttpClientFactory _httpClientFactory;
    public OrganisationDeleteForInactiveRegistrationJob(AppSettings appSettings, 
      ICacheInvalidateService cacheInvalidateService,
      IWrapperOrganisationService wrapperOrganisationService,
      IWrapperUserService wrapperUserService, 
      IHttpClientFactory httpClientFactory)
    {
      _appSettings = appSettings;
      _cacheInvalidateService = cacheInvalidateService;
      _wrapperOrganisationService = wrapperOrganisationService;
      _wrapperUserService = wrapperUserService;
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

            string activeUserId = string.Empty;
            var adminUsers = await GetOrganisationAdmins(orgDetail.Id);

            Console.WriteLine($"{adminUsers.Count()} org admin(s) found in Org id {orgDetail.OrganisationId}");

            foreach (var adminUser in adminUsers)
            {
              var url = "/security/users?email=" + HttpUtility.UrlEncode(adminUser.UserName);
              var response = await client.GetAsync(url);
              var responseContent = await response.Content.ReadAsStringAsync();
              var idamUser = JsonConvert.DeserializeObject<IdamUser>(responseContent);
              // var idamUser = await _wrapperSecurityService.GetUserByEmail(adminUser.UserName);

              if (idamUser != null)
              {

                if (idamUser.EmailVerified)
                {
                  orgDeleteCandidateStatus = OrgDeleteCandidateStatus.Activate;
                  activeUserId = adminUser.Id.ToString();
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
              await DeleteOrganisationAsync(orgDetail.OrganisationId);

              await DeleteCIIOrganisationEntryAsync(orgDetail.OrganisationId);

              if (_appSettings.OrgAutoValidationJobSettings.Enable && orgDetail.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier)
              {
                var orgStatus = new OrganisationAuditInfo
                {
                  Status = OrgAutoValidationStatus.AutoOrgRemoval,
                  CiiOrganisationId = orgDetail.OrganisationId,
                  Actioned = OrganisationAuditActionType.Job.ToString(),
                  ActionedBy = OrganisationAuditActionType.Job.ToString()
                };
                var organisationAudit = await _wrapperOrganisationService.UpdateOrganisationAuditList(orgStatus);

                var eventLogs = new List<OrganisationAuditEventInfo>() {
                    new OrganisationAuditEventInfo
                    {
                      Actioned = OrganisationAuditActionType.Job.ToString(),
                      Event = OrganisationAuditEventType.InactiveOrganisationRemoved.ToString(),
                      GroupId = groupId,
                      CiiOrganisationId = orgDetail.OrganisationId
                    }
                  };

                await _wrapperOrganisationService.CreateOrganisationAuditEventAsync(eventLogs);
              }
            }
            else if (orgDeleteCandidateStatus == OrgDeleteCandidateStatus.Activate)
            {
              //Console.WriteLine($"*********Activating CII Organization id {orgDetail.Item1} ***********************");
              await ActivateOrganisationAsync(activeUserId);
              //Console.WriteLine($"*********Activated CII Organization id {orgDetail.Item1} ***********************");
            }
          }
          catch (Exception e)
          {
            Console.WriteLine($"Failed to processed {i}st Organisation from the list of Organisations");

            Console.WriteLine($"Org deletion error " + JsonConvert.SerializeObject(e));
            //Console.WriteLine($"*********Error deleting Organization***********************" + e.Message);
          }
        }
      }
    }

    public async Task ActivateOrganisationAsync(string activeUserId)
    {
      await _wrapperOrganisationService.ActivateOrganisationByUser(activeUserId);
    }

    public async Task DeleteOrganisationAsync(string ciiOrgId)
    {
      Console.WriteLine($"*********Start Deleting from Conclave Organization id {ciiOrgId}***********************");
      await _wrapperOrganisationService.DeleteOrganisationAsync(ciiOrgId);
      Console.WriteLine($"*********End Deleted from Conclave Organization id {ciiOrgId}***********************");

      var filter = new UserFilterCriteria
      {
        isAdmin = true,
        includeSelf = true,
        includeUnverifiedAdmin = true,
        isDelegatedExpiredOnly = false,
        isDelegatedOnly = false,
        searchString = String.Empty
      };

      Console.WriteLine($"*********Start Deleting from Organization Users ***********************");
      var orgUsers = await _wrapperOrganisationService.GetUserByOrganisation(ciiOrgId, filter);
      orgUsers.ForEach(user =>
      {
        Console.WriteLine($"********* Deleting from Users: {user.UserName} ***********************");
        _wrapperUserService.DeleteUserAsync(user.UserName);
        _cacheInvalidateService.RemoveUserCacheValuesOnDeleteAsync(user.UserName, ciiOrgId, new List<int>());
      });

      var contactDetails = await _wrapperOrganisationService.GetOrganisationContactsList(ciiOrgId);
      if (contactDetails.ContactPoints.Any())
      {
        var orgContactPointIds = contactDetails.ContactPoints.Select(cp => cp.ContactPointId).ToList<int>();
        await _cacheInvalidateService.RemoveOrganisationCacheValuesOnDeleteAsync(ciiOrgId, orgContactPointIds, new Dictionary<string, List<int>>());
      }

    }

    public async Task<List<InactiveOrganisationResponse>> GetInactiveOrganisationAsync()
    {
      var createdOnUtc = DateTime.UtcNow.AddMinutes(-1 * _appSettings.ScheduleJobSettings.OrganizationRegistrationExpiredThresholdInMinutes);
      var organisationIds = await _wrapperOrganisationService.GetInactiveOrganisationAsync(createdOnUtc);
      return organisationIds;
    }

    public async Task DeleteCIIOrganisationEntryAsync(string ciiOrgId)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("x-api-key", _appSettings.CiiSettings.Token);
      client.BaseAddress = new Uri(_appSettings.CiiSettings.Url);
      var url = "identities/organisations/" + ciiOrgId;
      var result = await client.DeleteAsync(url);
      if (result.IsSuccessStatusCode)
      {
        //Console.WriteLine($"*********Deleted from CII Organization id {ciiOrgId} ***********************");
      }
      else
      {
        //Console.WriteLine($"*********Could not delete from CII Organization id {ciiOrgId} ***********************");
      }
    }

    private async Task<List<UserListForOrganisationInfo>> GetOrganisationAdmins(int organisationId)
    {

      var filter = new UserFilterCriteria
      {
        isAdmin = true,
        includeSelf = true,
        includeUnverifiedAdmin = false,
        isDelegatedExpiredOnly = false,
        isDelegatedOnly = false,
        searchString = String.Empty
      };

      var orgAdmins = await _wrapperUserService.GetUsersByOrganisation(organisationId, filter);
      return orgAdmins;
    }
  }
}
