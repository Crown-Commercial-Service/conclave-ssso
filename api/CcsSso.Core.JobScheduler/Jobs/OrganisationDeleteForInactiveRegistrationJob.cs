using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Enum;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
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
  public class OrganisationDeleteForInactiveRegistrationJob : BackgroundService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheInvalidateService _cacheInvalidateService;
    private readonly IIdamSupportService _idamSupportService;
    private readonly IOrganisationAuditService _organisationAuditService;
    private readonly IOrganisationAuditEventService _organisationAuditEventService;

    public OrganisationDeleteForInactiveRegistrationJob(IServiceScopeFactory factory, IDateTimeService dataTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory, ICacheInvalidateService cacheInvalidateService,
      IIdamSupportService idamSupportService)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _appSettings = appSettings;
      _httpClientFactory = httpClientFactory;
      _cacheInvalidateService = cacheInvalidateService;
      _idamSupportService = idamSupportService;
      _organisationAuditService = factory.CreateScope().ServiceProvider.GetRequiredService<IOrganisationAuditService>();
      _organisationAuditEventService = factory.CreateScope().ServiceProvider.GetRequiredService<IOrganisationAuditEventService>();
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
      var organisationIds = await GetExpiredOrganisationIdsAsync();
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
            //Get admin users to check their statuses in idam
            var adminUsers = await GetOrganisationAdmins(orgDetail.Id);
            
            Console.WriteLine($"{adminUsers.Count()} org admin(s) found in Org id {orgDetail.Id}");
            
            foreach (var adminUser in adminUsers)
            {
              var url = "/security/users?email=" + HttpUtility.UrlEncode(adminUser.UserName);
              var response = await client.GetAsync(url);
              if (response.StatusCode == System.Net.HttpStatusCode.OK)
              {
                var responseContent = await response.Content.ReadAsStringAsync();
                var idamUser = JsonConvert.DeserializeObject<IdamUser>(responseContent);

                if (idamUser.EmailVerified)
                {
                  orgDeleteCandidateStatus = OrgDeleteCandidateStatus.Activate;
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
              //Console.WriteLine($"*********Start Deleting from Conclave Organization id {orgDetail.Id}***********************");
              await DeleteOrganisationAsync(orgDetail.Id);
              //Console.WriteLine($"*********End Deleted from Conclave Organization id {orgDetail.Id}***********************");

              //Console.WriteLine($"*********Start Deleting from CII Organization id {orgDetail.Id} ***********************");
              await DeleteCIIOrganisationEntryAsync(orgDetail.CiiOrganisationId);
              //Console.WriteLine($"*********End Deleting from CII Organization id {orgDetail.Id} ***********************");

              if (_appSettings.OrgAutoValidationJobSettings.Enable && orgDetail.SupplierBuyerType != (int)RoleEligibleTradeType.Supplier)
              {
                var organisationAudit = _dataContext.OrganisationAudit.FirstOrDefault(x => x.OrganisationId == orgDetail.Id);
                if (organisationAudit != null)
                {
                  var orgStatus = new OrganisationAuditInfo
                  {
                    Status = OrgAutoValidationStatus.AutoOrgRemoval,
                    OrganisationId = orgDetail.Id,
                    Actioned = OrganisationAuditActionType.Job.ToString(),
                    ActionedBy = OrganisationAuditActionType.Job.ToString()
                  };

                  var eventLogs = new List<OrganisationAuditEventInfo>() {
                                new OrganisationAuditEventInfo
                                {
                                  Actioned = OrganisationAuditActionType.Job.ToString(),
                                  Event = OrganisationAuditEventType.InactiveOrganisationRemoved.ToString(),
                                  GroupId = groupId,
                                  OrganisationId = orgDetail.Id
                                }
                              };

                  await _organisationAuditService.UpdateOrganisationAuditAsync(orgStatus);
                  await _organisationAuditEventService.CreateOrganisationAuditEventAsync(eventLogs);
                }
              }
            }
            else if (orgDeleteCandidateStatus == OrgDeleteCandidateStatus.Activate)
            {
              //Console.WriteLine($"*********Activating CII Organization id {orgDetail.Item1} ***********************");
              await ActivateOrganisationAsync(orgDetail.Id);
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

    public async Task ActivateOrganisationAsync(int orgId)
    {
      var org = await _dataContext.Organisation.FirstOrDefaultAsync(o => o.Id == orgId);
      if (org != null)
      {
        org.IsActivated = true;
      }
      await _dataContext.SaveChangesAsync();
    }

    public async Task DeleteOrganisationAsync(int orgId)
    {
      var deletingOrganisation = await _dataContext.Organisation
                                .Include(o => o.OrganisationEligibleIdentityProviders)
                                .Include(o => o.OrganisationAccessRoles)
                                .Include(o => o.OrganisationEligibleRoles)
                                .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
                                .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
                                .FirstOrDefaultAsync(o => o.Id == orgId);

      if (deletingOrganisation != null)
      {
        List<int> orgContactPointIds = new();
        Dictionary<string, List<int>> deletingUserDetails = new();

        deletingOrganisation.OrganisationEligibleIdentityProviders.ForEach((idp) => { idp.IsDeleted = true; });

        if (deletingOrganisation.Party != null)
        {
          deletingOrganisation.Party.IsDeleted = true;
        }

        deletingOrganisation.IsDeleted = true;

        if (deletingOrganisation.Party.ContactPoints != null)
        {
          foreach (var orgContactPoint in deletingOrganisation.Party.ContactPoints)
          {
            orgContactPoint.IsDeleted = true;
            orgContactPointIds.Add(orgContactPoint.Id);
            if (orgContactPoint.ContactDetail != null)
            {
              orgContactPoint.ContactDetail.IsDeleted = true;
            }

            if (orgContactPoint.ContactDetail.PhysicalAddress != null)
            {
              orgContactPoint.ContactDetail.PhysicalAddress.IsDeleted = true;
            }
          }
        }

        if (deletingOrganisation.OrganisationAccessRoles != null)
        {
          foreach (var orgAccessRoles in deletingOrganisation.OrganisationAccessRoles)
          {
            orgAccessRoles.IsDeleted = true;
          }
        }

        if (deletingOrganisation.OrganisationEligibleRoles != null)
        {
          foreach (var orgEligibleRoles in deletingOrganisation.OrganisationEligibleRoles)
          {
            orgEligibleRoles.IsDeleted = true;
          }
        }

        var deletingOrganisationPeople = await _dataContext.Organisation
        .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.User).ThenInclude(u => u.UserAccessRoles)
        .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.User).ThenInclude(u => u.UserIdentityProviders)
        .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.ContactPoints)
        .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .FirstOrDefaultAsync(o => o.Id == orgId);

        if (deletingOrganisationPeople.People != null)
        {
          foreach (var person in deletingOrganisationPeople.People)
          {
            person.Party.IsDeleted = true;
            person.IsDeleted = true;

            if (person.Party.User != null)
            {
              person.Party.User.IsDeleted = true;
              if (person.Party.User.UserAccessRoles != null)
              {
                foreach (var uar in person.Party.User.UserAccessRoles)
                {
                  uar.IsDeleted = true;
                }
              }

              if (person.Party.User.UserIdentityProviders != null)
              {
                foreach (var uidp in person.Party.User.UserIdentityProviders.Where(uidp => !uidp.IsDeleted))
                {
                  uidp.IsDeleted = true;
                }
              }

              List<int> delteingUserContactPointIds = new();
              if (person.Party.ContactPoints != null)
              {
                foreach (var personContactPoint in person.Party.ContactPoints)
                {
                  personContactPoint.IsDeleted = true;
                  delteingUserContactPointIds.Add(personContactPoint.Id);
                  if (personContactPoint.ContactDetail != null)
                  {
                    personContactPoint.ContactDetail.IsDeleted = true;

                    if (personContactPoint.ContactDetail.VirtualAddresses != null)
                    {
                      foreach (var virtualContact in personContactPoint.ContactDetail.VirtualAddresses)
                      {
                        virtualContact.IsDeleted = true;
                      }
                    }
                  }
                }
              }

              deletingUserDetails.Add(person.Party.User.UserName, delteingUserContactPointIds);
            }
          }
        }
        await _dataContext.SaveChangesAsync();
        foreach (var user in deletingUserDetails)
        {
          //Console.WriteLine($"********* Deleting {userName} from Auth0 ***********************");
          await _idamSupportService.DeleteUserInIdamAsync(user.Key);
          try
          {
            await _cacheInvalidateService.RemoveUserCacheValuesOnDeleteAsync(user.Key, deletingOrganisation.CiiOrganisationId, user.Value);
          }
          catch // Dont want to stop user deletion if any error in cache invalidate
          {
            continue;
          }
        }
        await _cacheInvalidateService.RemoveOrganisationCacheValuesOnDeleteAsync(deletingOrganisation.CiiOrganisationId, orgContactPointIds,
          new Dictionary<string, List<int>>());
      }
    }

    public async Task<List<Organisation>> GetExpiredOrganisationIdsAsync()
    {
      var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted
                          && org.CreatedOnUtc < _dataTimeService.GetUTCNow().AddMinutes(-(_appSettings.ScheduleJobSettings.OrganizationRegistrationExpiredThresholdInMinutes)))
                          .ToListAsync();

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

    private async Task<List<User>> GetOrganisationAdmins(int organisationId)
    {
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
       .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId &&
       or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey))?.Id;

      var orgAdmins = await _dataContext.User.Where(u => !u.IsDeleted
       && u.Party.Person.OrganisationId == organisationId
       && (u.UserGroupMemberships.Any(ugm => !ugm.IsDeleted
       && ugm.OrganisationUserGroup.GroupEligibleRoles.Any(ga => !ga.IsDeleted && ga.OrganisationEligibleRoleId == orgAdminAccessRoleId))
       || u.UserAccessRoles.Any(ur => !ur.IsDeleted && ur.OrganisationEligibleRoleId == orgAdminAccessRoleId)))
      .Select(u => u).OrderBy(u => u.Id).ToListAsync();

      return orgAdmins;
    }
  }
}
