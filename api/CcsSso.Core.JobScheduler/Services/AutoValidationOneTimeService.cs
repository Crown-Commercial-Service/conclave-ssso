using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Model;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Domain.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VaultSharp.V1.SecretsEngines.AWS;
using OrganisationDetail = CcsSso.Core.JobScheduler.Model.OrganisationDetail;

namespace CcsSso.Core.JobScheduler.Services
{
  public class AutoValidationOneTimeService : IAutoValidationOneTimeService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AutoValidationService> _logger;
    private readonly IDataContext _dataContext;


    public AutoValidationOneTimeService(IHttpClientFactory httpClientFactory, AppSettings appSettings, IServiceScopeFactory factory,
      ILogger<AutoValidationService> logger)
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

          if (orgDetail.SupplierBuyerType == 0)
          {
            var buyerRoles = _appSettings.OrgAutoValidationOneTimeJobRoles.RemoveBuyerRoleFromSupplierOrg;
            await RemoveRoles(client, buyerRoles, orgDetail, logMessage);

            var supplierRoles = _appSettings.OrgAutoValidationOneTimeJobRoles.AddRolesToSupplierOrg;
            await AddRoles(client, supplierRoles, orgDetail, logMessage);

          }
          else if (orgDetail.SupplierBuyerType == 1)
          {
            var supplierRoles = _appSettings.OrgAutoValidationOneTimeJobRoles.RemoveRoleFromBuyerOrg;
            await RemoveRoles(client, supplierRoles, orgDetail, logMessage);
          }
          else if (orgDetail.SupplierBuyerType == 2)
          {
            var addBothRoles = _appSettings.OrgAutoValidationOneTimeJobRoles.AddRolesToBothOrgOnly;
            await AddRoles(client, addBothRoles, orgDetail, logMessage);
          }
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
      try
      {
        _logger.LogInformation($"Removing roles from Org. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}");


        var rolesToDeleteIds = _dataContext.CcsAccessRole.Where(oer => roles.Contains(oer.CcsAccessRoleNameKey)).Select(x => x.Id).ToList();

        var RolesToDelete = new List<OrganisationRole>();

        foreach (var id in rolesToDeleteIds)
        {
          RolesToDelete.Add(new OrganisationRole() { RoleId = id });
        }

        var updateRoles = new OrganisationRoleUpdate() { IsBuyer = (bool)orgDetail.RightToBuy, RolesToDelete = RolesToDelete };

        var url = "/organisations/" + orgDetail.CiiOrganisationId + "/roles";
        HttpContent data = new StringContent(JsonConvert.SerializeObject(updateRoles, new JsonSerializerSettings
        { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, data);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
          _logger.LogInformation($"Role removed successfully for the org CiiId =" + orgDetail.CiiOrganisationId);
          AddLogMessage(logMessage, orgDetail, $"Successfully removed roles {string.Join(',', roles)}");
        }
        else
        {
          _logger.LogWarning($"Failed to remove role {string.Join(',', roles)}. Org CiiId =" + orgDetail.CiiOrganisationId);
          _logger.LogWarning($"Failed to remove role {string.Join(',', roles)}. Response =" + response.Content);
          AddLogMessage(logMessage, orgDetail, $"Failed to remove roles {string.Join(',', roles)}");
        }
      }
      catch (Exception)
      {
        _logger.LogError($"Exception while removing role {string.Join(',', roles)} from org. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}");

        AddLogMessage(logMessage, orgDetail, $"Failed to remove roles {string.Join(',', roles)}");

      }
    }

    private async Task AddRoles(HttpClient client, string[] roles, OrganisationDetail orgDetail, List<OrganisationLogDetail> logMessage)
    {
      try
      {
        _logger.LogInformation($"Add roles to Org. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}");


        var rolesToAddIds = _dataContext.CcsAccessRole.Where(oer => roles.Contains(oer.CcsAccessRoleNameKey)).Select(x => x.Id).ToList();

        var RolesToAdd = new List<OrganisationRole>();

        foreach (var id in rolesToAddIds)
        {
          RolesToAdd.Add(new OrganisationRole() { RoleId = id });
        }

        var updateRoles = new OrganisationRoleUpdate() { IsBuyer = (bool)orgDetail.RightToBuy, RolesToAdd = RolesToAdd };

        var url = "/organisations/" + orgDetail.CiiOrganisationId + "/roles";
        HttpContent data = new StringContent(JsonConvert.SerializeObject(updateRoles, new JsonSerializerSettings
        { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, data);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
          _logger.LogInformation($"Role {string.Join(',', roles)} added successfully for the org CiiId =" + orgDetail.CiiOrganisationId);
          AddLogMessage(logMessage, orgDetail, $"Successfully added roles {string.Join(',', roles)}");
        }
        else
        {
          _logger.LogWarning($"Failed to add roles {string.Join(',', roles)}. Org CiiId =" + orgDetail.CiiOrganisationId);
          _logger.LogWarning($"Failed to add roles {string.Join(',', roles)}. Response =" + response.Content);
          AddLogMessage(logMessage, orgDetail, $"Failed to add roles {string.Join(',', roles)}");
        }
      }
      catch (Exception)
      {
        _logger.LogError($"Exception while adding roles {string.Join(',', roles)} to org. LegalName: {orgDetail.LegalName}, CiiOrgId={orgDetail.CiiOrganisationId}, Id={orgDetail.Id}");
        AddLogMessage(logMessage, orgDetail, $"Failed to add roles {string.Join(',', roles)}");

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
