using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Jobs
{
  public class OrganisationDeleteForInactiveRegistrationJob : IJob
  {
    private readonly IDataContext _dataContext;
    private readonly IDataTimeService _dataTimeService;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrganisationDeleteForInactiveRegistrationJob(IDataContext dataContext, IDataTimeService dataTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
      _dataContext = dataContext;
      _dataTimeService = dataTimeService;
      _appSettings = appSettings;
      _httpClientFactory = httpClientFactory;
    }

    public async Task PerformJobAsync()
    {
      var organisationIds = await GetExpiredOrganisationRegistrationsIdsAsync();
      Console.WriteLine($"Found {organisationIds.Count()} organizations");
      if (organisationIds != null)
      {
        foreach (var id in organisationIds)
        {
          Console.WriteLine($"organization {id} will be deleted");
          await DeleteOrganisationAsync(id);
          await DeleteCIIOrganisationEntryAsync(id);
        }
      }
    }

    public async Task DeleteOrganisationAsync(string ciiOrganisationId)
    {
      List<string> userNames = new List<string>();

      var deletingOrganisation = await _dataContext.Organisation
        .Include(o => o.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
        .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

      if (deletingOrganisation != null)
      {
        deletingOrganisation.Party.IsDeleted = true;
        deletingOrganisation.IsDeleted = true;
        if (deletingOrganisation.Party.ContactPoints != null)
        {
          foreach (var orgContactPoint in deletingOrganisation.Party.ContactPoints)
          {
            orgContactPoint.IsDeleted = true;
            orgContactPoint.ContactDetail.IsDeleted = true;
            orgContactPoint.ContactDetail.PhysicalAddress.IsDeleted = true;
          }
        }

        var deletingOrganisationPeople = await _dataContext.Organisation
        .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.User)
        .Include(o => o.People).ThenInclude(prs => prs.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
        .FirstOrDefaultAsync(o => o.CiiOrganisationId == ciiOrganisationId);

        if (deletingOrganisationPeople.People != null)
        {
          foreach (var person in deletingOrganisationPeople.People)
          {
            person.Party.IsDeleted = true;

            if (person.Party.User != null)
            {
              person.Party.User.IsDeleted = true;
              userNames.Add(person.Party.User.UserName); // Add the userName to delete from Auth0
            }

            if (person.Party.ContactPoints != null)
            {
              foreach (var personContactPoint in person.Party.ContactPoints)
              {
                personContactPoint.IsDeleted = true;
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
        await _dataContext.SaveChangesAsync();
        Console.WriteLine($"organization data are deleted");
        foreach (var userName in userNames)
        {
          await DeleteUserFromSecurityApiAsync(userName);
        }
      }
    }

    public async Task<List<string>> GetExpiredOrganisationRegistrationsIdsAsync()
    {
      var ciiOrganisationIds = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted
                          && org.CreatedOnUtc < _dataTimeService.GetUTCNow().AddMinutes(-(_appSettings.ScheduleJobSettings.OrganizationRegistrationExpiredThresholdInMinutes)))
                          .Select(o => o.CiiOrganisationId).ToListAsync();

      return ciiOrganisationIds;
    }

    public async Task DeleteCIIOrganisationEntryAsync(string ciiOrgId)
    {
      Console.WriteLine($"CII Org {ciiOrgId} will be deleted");
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", _appSettings.DbConnection);
      client.BaseAddress = new Uri(_appSettings.CiiSettings.BaseURL);
      var url = "/identities/schemes/organisation?ccs_org_id=" + ciiOrgId;
      await client.DeleteAsync(url);
      Console.WriteLine($"CII Org {ciiOrgId} is deleted");
    }

    private async Task DeleteUserFromSecurityApiAsync(string userName)
    {
      //Delete the user from IDAM via Security api
      Console.WriteLine($"User {userName} will be deleted");
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-API-Key", _appSettings.SecurityApiSettings.ApiKey);
      client.BaseAddress = new Uri(_appSettings.SecurityApiSettings.Url);
      var url = "/security/deleteuser";
      var data = new StringContent($"\"{userName}\"", Encoding.UTF8, "application/json");
      await client.PostAsync(url, data);
      Console.WriteLine($"User {userName} is deleted");
    }
  }
}
