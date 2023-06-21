using CcsSso.Core.PPONScheduler.Model;
using CcsSso.Core.PPONScheduler.Service.Contracts;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CcsSso.Core.PPONScheduler.Service
{
  public class PPONService : IPPONService
  {
    private readonly IDataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PPONAppSettings _appSettings;
    private readonly IDateTimeService _dataTimeService;
    private readonly ICiiService _ciiService;
    private readonly ILogger<IPPONService> _logger;

    public PPONService(ILogger<IPPONService> logger, IServiceScopeFactory factory,
      PPONAppSettings appSettings, IDateTimeService dataTimeService,
      IHttpClientFactory httpClientFactory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _ciiService = factory.CreateScope().ServiceProvider.GetRequiredService<ICiiService>();
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
    }

    public async Task PerformJob(bool oneTimeValidationSwitch, DateTime startDate, DateTime endDate)
    {
      try
      {
        var listOfRegisteredOrgs = await GetRegisteredOrgsAsync(oneTimeValidationSwitch, startDate, endDate);

        if (listOfRegisteredOrgs == null || listOfRegisteredOrgs.Count() == 0)
        {
          _logger.LogInformation("No Organisation found");
          return;
        }

        _logger.LogInformation($"Number of organisation {listOfRegisteredOrgs.Count()}");

        foreach (var orgDetails in listOfRegisteredOrgs)
        {
          _logger.LogInformation("------------------------------------------------------------------------------------");
          await ProcessOrg(orgDetails);
          _logger.LogInformation("------------------------------------------------------------------------------------");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Outer Exception during this schedule, exception message =  {ex.Message}");
      }
    }

    private async Task ProcessOrg(Organisation orgDetails)
    {
      var ciiOrgId = orgDetails.CiiOrganisationId;
      var orgLegalName = orgDetails.LegalName;

      try
      {
        _logger.LogInformation($"Org: {ciiOrgId + " - " + orgLegalName}");

        await LinkPPONWithOrgAsync(ciiOrgId);
      }
      catch (Exception ex)
      {
        _logger.LogError($"*****Inner Exception while processing the org: {ciiOrgId}, exception message =  {ex.Message}");
      }
    }

    private async Task<List<Organisation>> GetRegisteredOrgsAsync(bool oneTimeValidationSwitch, DateTime startDate, DateTime endDate)
    {
      var dataDuration = _appSettings.ScheduleJobSettings.DataDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        if (oneTimeValidationSwitch)
        {
          return await GetRegisteredOrgsByDateAsync(startDate, endDate);
        }
        else
        {
          return await GetRegisteredOrgsByDateAsync(untilDateTime);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }

    private async Task<List<Organisation>> GetRegisteredOrgsByDateAsync(DateTime untilDateTime)
    {
      var result = await _dataContext.Organisation.Where(
                        org => !org.IsDeleted).ToListAsync();

      return result.Where(org => org.CreatedOnUtc > untilDateTime)
                    .Select(o =>
                    new Organisation
                    {
                      Id = o.Id,
                      CiiOrganisationId = o.CiiOrganisationId,
                      LegalName = o.LegalName,
                      CreatedOnUtc = o.CreatedOnUtc,
                      RightToBuy = o.RightToBuy,
                      SupplierBuyerType = o.SupplierBuyerType
                    }).ToList();
    }

    private async Task<List<Organisation>> GetRegisteredOrgsByDateAsync(DateTime startDate, DateTime endDate)
    {
      return await _dataContext.Organisation.Where(
                                  org => !org.IsDeleted
                                  && org.CreatedOnUtc >= TimeZoneInfo.ConvertTimeToUtc(startDate) && org.CreatedOnUtc <= TimeZoneInfo.ConvertTimeToUtc(endDate))
                  .Select(o => new Organisation
                  {
                    Id = o.Id,
                    CiiOrganisationId = o.CiiOrganisationId,
                    LegalName = o.LegalName,
                    CreatedOnUtc = o.CreatedOnUtc,
                    RightToBuy = o.RightToBuy,
                    SupplierBuyerType = o.SupplierBuyerType
                  }).ToListAsync();
    }

    private async Task LinkPPONWithOrgAsync(string ciiOrganisationId)
    {
      try
      {
        _logger.LogInformation("Try to get organisation details from CII");

        var isExist = await IsPPONExistsAsync(ciiOrganisationId);

        if (isExist)
        {
          _logger.LogInformation("PPON Id already exists");
        }
        else
        {
          var pPONDetails = await GetPPONDetailsAsync(ciiOrganisationId);
          await UpdatePPONAsync(ciiOrganisationId, pPONDetails);

          _logger.LogInformation("PPON Id updated successfully");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }

    private async Task UpdatePPONAsync(string ciiOrganisationId, PPONDetails pPONDetails)
    {
      if (pPONDetails != null && pPONDetails.Identifiers != null && pPONDetails.Identifiers.Count > 0)
      {
        var id = pPONDetails.Identifiers.FirstOrDefault().Id;
        _logger.LogInformation("PPON Id: {0}", id);
        await _ciiService.AddSchemeAsync(ciiOrganisationId, "GB-PPG", id, null);
      }
      else
      {
        _logger.LogInformation("PPON Id not found");
      }
    }

    private async Task<bool> IsPPONExistsAsync(string ciiOrganisationId)
    {
      var isExist = false;

      _logger.LogInformation("Try to get organisation details from CII");

      CiiDto organisationInfo = await _ciiService.GetOrgDetailsAsync(ciiOrganisationId);

      if (organisationInfo != null)
      {
        _logger.LogInformation("Organisation found on CII database");

        isExist = organisationInfo?.AdditionalIdentifiers?.Any(x => x.Scheme == "GB-PPG") ?? false;
      }
      else
      {
        _logger.LogInformation("Organisation not found on CII database");
      }

      return isExist;
    }

    private async Task<PPONDetails> GetPPONDetailsAsync(string ciiOrganisationId)
    {
      var client = _httpClientFactory.CreateClient("PPONApi");
      var response = await client.PostAsync("identifiers/id/ppon", null);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<PPONDetails>(content);
      }
      else
      {
        _logger.LogError("Error while getting PPON details: {0}", response.StatusCode);
        throw new Exception("ERROR_RETRIEVING_PPON_DETAILS");
      }
    }
  }
}

