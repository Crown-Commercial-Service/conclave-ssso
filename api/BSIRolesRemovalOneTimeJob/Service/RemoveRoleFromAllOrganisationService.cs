using CcsSso.Core.BSIRolesRemovalOneTimeJob.Contracts;
using System.Text;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
//using CcsSso.Core.JobScheduler.Contracts;
//using CcsSso.Core.JobScheduler.Model;
using CcsSso.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OrganisationDetail = CcsSso.Core.BSIRolesRemovalOneTimeJob.Model.OrganisationDetail;
using CcsSso.Core.BSIRolesRemovalOneTimeJob.Model;

namespace CcsSso.Core.BSIRolesRemovalOneTimeJob.Service
{
  public class RemoveRoleFromAllOrganisationService:IRemoveRoleFromAllOrganisationService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<RemoveRoleFromAllOrganisationService> _logger;
    private readonly IDataContext _dataContext;

    public RemoveRoleFromAllOrganisationService(IHttpClientFactory httpClientFactory, AppSettings appSettings, IServiceScopeFactory factory,
     ILogger<RemoveRoleFromAllOrganisationService> logger)
    {
      _httpClientFactory = httpClientFactory;
      _appSettings = appSettings;
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _logger = logger;

    }

    public async Task PerformJobAsync(List<OrganisationDetail> organisations)
    {
      if (organisations == null)
      {
        _logger.LogWarning($"No organisation found");
        return;
      }

      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.WrapperApiSettings.ApiKey);
      client.BaseAddress = new Uri(_appSettings.WrapperApiSettings.Url);

      var logMessage = new List<OrganisationLogDetail>();

      foreach (var orgDetail in organisations)
      {
        try
        {
          var bothRoles = _appSettings.OrgAutoValidationOneTimeJobRoles.RemoveRoleFromAllOrg;
          await RemoveRoles(client, bothRoles, orgDetail, logMessage);

        }
        catch (Exception e)
        {
          _logger.LogError($"Exception while Add/Remove roles. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}, Exception Message=   {JsonConvert.SerializeObject(e)}");
          AddLogMessage(logMessage, orgDetail, "Failed to add/Remove roles");
          continue;
        }
      }

      _logger.LogInformation(JsonConvert.SerializeObject(logMessage));

    }

    private async Task RemoveRoles(HttpClient client, string[] roles, OrganisationDetail orgDetail, List<OrganisationLogDetail> logMessage)
    {
      var eligibleRolesToDelete = new List<int>();
      try
      {
        _logger.LogInformation($"Removing roles from Org. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}");

        var organisation = await _dataContext.Organisation
        .Include(er => er.OrganisationEligibleRoles)
       .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == orgDetail.CiiOrganisationId);

        var rolesToDeleteIds = _dataContext.CcsAccessRole.Where(oer => roles.Contains(oer.CcsAccessRoleNameKey)).Select(x => x.Id).ToList();

        eligibleRolesToDelete = rolesToDeleteIds.Intersect(organisation.OrganisationEligibleRoles.Where(oer => !oer.IsDeleted).Select(y => y.CcsAccessRoleId)).ToList();

        if (!eligibleRolesToDelete.Any())
        {
          _logger.LogWarning($"No roles are deleted. Roles {string.Join(',', eligibleRolesToDelete)} doesn't exist.");
          AddLogMessage(logMessage, orgDetail, $"No roles are deleted. Roles {string.Join(',', eligibleRolesToDelete)} doesn't exist.");
          return;
        }


        var RolesToDelete = new List<OrganisationRole>();

        foreach (var id in eligibleRolesToDelete)
        {
          RolesToDelete.Add(new OrganisationRole() { RoleId = id });
        }

        var updateRoles = new OrganisationRoleUpdate() { IsBuyer = (bool)orgDetail.RightToBuy, RolesToDelete = RolesToDelete };

        var url = "/organisations/" + orgDetail.CiiOrganisationId + "/roles";
        HttpContent data = new StringContent(JsonConvert.SerializeObject(updateRoles, new JsonSerializerSettings
        { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

        var response = await client.PutAsync(url, data);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
          _logger.LogInformation($"Role removed successfully for the org CiiId =" + orgDetail.CiiOrganisationId);
          AddLogMessage(logMessage, orgDetail, $"Successfully removed roles {string.Join(',', eligibleRolesToDelete)}");
        }
        else
        {
          _logger.LogWarning($"Failed to add roles {string.Join(',', eligibleRolesToDelete)}. Response StatusCode =" + response.StatusCode);
          _logger.LogWarning($"Failed to remove role {string.Join(',', eligibleRolesToDelete)}. Org CiiId =" + orgDetail.CiiOrganisationId);
          _logger.LogWarning($"Failed to remove role {string.Join(',', eligibleRolesToDelete)}. Response =" + response.Content);
          AddLogMessage(logMessage, orgDetail, $"Failed to remove roles {string.Join(',', eligibleRolesToDelete)}");
        }
      }
      catch (Exception)
      {
        _logger.LogError($"Exception while removing role {string.Join(',', eligibleRolesToDelete)} from org. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}");

        AddLogMessage(logMessage, orgDetail, $"Failed to remove roles {string.Join(',', eligibleRolesToDelete)}");

      }
    }

    private void AddLogMessage(List<OrganisationLogDetail> logMessage, OrganisationDetail orgDetail, string information)
    {
      logMessage.Add(new OrganisationLogDetail()
      {
        Id = orgDetail.Id,
        LegalName = orgDetail.LegalName,
        CiiOrganisationId = orgDetail.CiiOrganisationId,
        RightToBuy = orgDetail.RightToBuy,
        SupplierBuyerType = orgDetail.SupplierBuyerType,
        Information = information
      });
    }


  }
}
