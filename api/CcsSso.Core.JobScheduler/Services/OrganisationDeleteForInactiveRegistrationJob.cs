using CcsSso.Core.Domain.Jobs;
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
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler
{
  public class OrganisationDeleteForInactiveRegistrationJob : BackgroundService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrganisationDeleteForInactiveRegistrationJob(IServiceScopeFactory factory, IDateTimeService dataTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _appSettings = appSettings;
      _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

      while (!stoppingToken.IsCancellationRequested)
      {
        Console.WriteLine($" ****************Organization batch processing job started ***********");
        await PerformJobAsync();
        await Task.Delay(_appSettings.ScheduleJobSettings.JobSchedulerExecutionFrequencyInMinutes * 60000, stoppingToken);
        Console.WriteLine($"******************Organization batch processing job ended ***********");
      }
    }

    private async Task PerformJobAsync()
    {
      var organisationIds = await GetExpiredOrganisationIdsAsync();
      //Console.WriteLine($"{organisationIds.Count()} organizations found");
      if (organisationIds != null)
      {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);
        client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);

        foreach (var orgDetail in organisationIds)
        {
          try
          {
            bool isCandidateToDelete = true;
            //Get admin users to check their statuses in idam
            var adminUsers = await GetOrganisationAdmins(orgDetail.Item1);
            //Console.WriteLine($"{adminUsers.Count()} org admin(s) found in Org id {orgDetail.Item2}");
            foreach (var adminUser in adminUsers)
            {
              var url = "/security/getuser?email=" + adminUser.UserName;
              var response = await client.GetAsync(url);
              if (response.StatusCode == System.Net.HttpStatusCode.OK)
              {
                var responseContent = await response.Content.ReadAsStringAsync();
                var idamUser = JsonConvert.DeserializeObject<IdamUser>(responseContent);

                if (idamUser.EmailVerified)
                {
                  isCandidateToDelete = false;
                  break;
                }
              }
            }

            if (isCandidateToDelete)
            {
              //Console.WriteLine($"*********Deleting from Conclave Organization id {orgDetail.Item1}***********************");
              await DeleteOrganisationAsync(orgDetail.Item1);
              //Console.WriteLine($"*********Deleted from Conclave Organization id {orgDetail.Item1}***********************");

              //Console.WriteLine($"*********Deleting from CII Organization id {orgDetail.Item1} ***********************");
              await DeleteCIIOrganisationEntryAsync(orgDetail.Item2);              
            }
            else
            {
              //Console.WriteLine($"*********Activating CII Organization id {orgDetail.Item1} ***********************");
              await ActivateOrganisationAsync(orgDetail.Item1);
              //Console.WriteLine($"*********Activated CII Organization id {orgDetail.Item1} ***********************");
            }
          }
          catch (Exception e)
          {
            //Console.WriteLine($"Org deletion error " + e.Message);
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
      List<string> userNames = new List<string>();

      var deletingOrganisation = await _dataContext.Organisation
                                .Include(o => o.OrganisationEligibleIdentityProviders)
                                .Include(o => o.OrganisationAccessRoles)
                                .Include(o => o.OrganisationEligibleRoles)
                                .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
                                .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
                                .FirstOrDefaultAsync(o => o.Id == orgId);

      if (deletingOrganisation != null)
      {
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
              userNames.Add(person.Party.User.UserName); // Add the userName to delete from Auth0
            }

            if (person.Party.ContactPoints != null)
            {
              foreach (var personContactPoint in person.Party.ContactPoints)
              {
                personContactPoint.IsDeleted = true;

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
          }
        }
        await _dataContext.SaveChangesAsync();
        foreach (var userName in userNames)
        {
          //Console.WriteLine($"********* Deleting {userName} from Auth0 ***********************");
          await DeleteUserFromSecurityApiAsync(userName);          
        }
      }
    }

    public async Task<List<Tuple<int, string>>> GetExpiredOrganisationIdsAsync()
    {
      var organisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted
                          && org.CreatedOnUtc < _dataTimeService.GetUTCNow().AddMinutes(-(_appSettings.ScheduleJobSettings.OrganizationRegistrationExpiredThresholdInMinutes)))
                          .Select(o => new Tuple<int, string>(o.Id, o.CiiOrganisationId)).ToListAsync();

      return organisationIds;
    }

    public async Task DeleteCIIOrganisationEntryAsync(string ciiOrgId)
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("x-api-key", _appSettings.CiiSettings.Token);
      client.BaseAddress = new Uri(_appSettings.CiiSettings.Url);
      var url = "/identities/organisation?ccs_org_id=" + ciiOrgId;
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

    private async Task DeleteUserFromSecurityApiAsync(string userName)
    {
      //Delete the user from IDAM via Security api
      //Console.WriteLine($"User {userName} will be deleted");
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);
      client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);
      var url = "/security/deleteuser?email=" + userName;
      var response = await client.DeleteAsync(url);
      if (response.StatusCode == System.Net.HttpStatusCode.OK)
      {
        //Console.WriteLine($"********* Deleted {userName} from Auth0 ***********************");
      }
      else
      {
        //Console.WriteLine($"********* Could not delete {userName} from Auth0 ***********************");
      }
    }

    private async Task<List<User>> GetOrganisationAdmins(int organisationId)
    {
      var orgAdminAccessRoleId = (await _dataContext.OrganisationEligibleRole
       .FirstOrDefaultAsync(or => !or.IsDeleted && or.OrganisationId == organisationId &&
       or.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)).Id;

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
