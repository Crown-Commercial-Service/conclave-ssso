using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Model;
using CcsSso.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler
{
  public class OrganisationAutovalidationJob : BackgroundService
  {
    private readonly IDataContext _dataContext;
    private readonly AppSettings _appSettings;
    private readonly IAutoValidationService _autoValidationService;
    private readonly IAutoValidationOneTimeService _autoValidationOneTimeService;

    private readonly ILogger<OrganisationAutovalidationJob> _logger;
    private bool enable;
    private bool ranOnce;
    private DateTime startDate;
    private DateTime endDate;

    public OrganisationAutovalidationJob(ILogger<OrganisationAutovalidationJob> logger, IServiceScopeFactory factory,
      AppSettings appSettings, IAutoValidationService autoValidationService, IAutoValidationOneTimeService autoValidationOneTimeService)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();

      _appSettings = appSettings;
      _autoValidationService = autoValidationService;
      _logger = logger;
      ranOnce = false;
      enable = false;
      _autoValidationOneTimeService = autoValidationOneTimeService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

      while (!stoppingToken.IsCancellationRequested)
      {
        enable = _appSettings.OrgAutoValidationOneTimeJob.Enable;
        int interval = _appSettings.ScheduleJobSettings.OrganisationAutovalidationJobExecutionFrequencyInMinutes * 60000;

        if (!enable)
        {
          _logger.LogInformation($"One time job is disabled. Skipping this iteration");
          await Task.Delay(interval, stoppingToken);
          continue;
        }

        var dates = ReadDateRange();

        if (dates == null)
        {
          await Task.Delay(interval, stoppingToken);
          continue;
        }

        startDate = dates.Item1;
        endDate = dates.Item2;

        if (ranOnce)
        {
          _logger.LogInformation("One time validation ran already. Skipping this iteration.");
          await Task.Delay(interval, stoppingToken);
          continue;
        }

        _logger.LogInformation($" ****************Organization autovalidation batch processing job started ***********");
        await PerformJobAsync();
        ranOnce = true;
        await Task.Delay(interval, stoppingToken);
        _logger.LogInformation($"******************Organization autovalidation batch processing job ended ***********");
      }
    }

    private async Task PerformJobAsync()
    {
      var organisations = await GetOrganisationsAsync();

      _logger.LogInformation($"Autovalidation Total Number of organisation found to remove/add roles: {organisations.Count()}");

      await _autoValidationOneTimeService.PerformJobAsync(organisations);

      var organisationsForAutovalidation = await GetOrganisationsForAutoValidationAsync();
      _logger.LogInformation($"Autovalidation Total Number of organisation found for autovalidation: {organisationsForAutovalidation.Count()}");

      await _autoValidationService.PerformJobAsync(organisationsForAutovalidation);


    }

    private Tuple<DateTime, DateTime> ReadDateRange()
    {
      var startDateString = _appSettings.OrgAutoValidationOneTimeJob.StartDate;
      var endDateString = _appSettings.OrgAutoValidationOneTimeJob.EndDate;

      if (startDateString == null || endDateString == null)
      {
        _logger.LogError("One time validation needs start and end date. Skipping this iteration.");
        return null;
      }

      try
      {
        startDate = DateTime.ParseExact(startDateString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(endDateString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

      }
      catch (FormatException)
      {
        _logger.LogError("{0} or {1} is not in the correct format. Date format should be as follows 'yyyy-MM-dd' Skipping this iteration.", startDateString, endDateString);
        return null;

      }
      catch (Exception)
      {
        _logger.LogError("Error while reading the start or end date {0}, {1}. Skipping this iteration.", startDateString, endDateString);
        return null;

      }
      return new Tuple<DateTime, DateTime>(startDate, endDate);
    }

    public async Task<List<OrganisationDetail>> GetOrganisationsAsync()
    {

      var organisations = await _dataContext.Organisation.Where(
                          org => !org.IsDeleted && org.CreatedOnUtc >= TimeZoneInfo.ConvertTimeToUtc(startDate) && org.CreatedOnUtc <= TimeZoneInfo.ConvertTimeToUtc(endDate))
                          .Select(o => new OrganisationDetail()
                          {
                            Id = o.Id,
                            CiiOrganisationId = o.CiiOrganisationId,
                            SupplierBuyerType = o.SupplierBuyerType.Value,
                            LegalName = o.LegalName,
                            RightToBuy = o.RightToBuy,
                            CreatedOnUtc = o.CreatedOnUtc
                          }).ToListAsync();

      return organisations;
    }


    public async Task<List<OrganisationDetail>> GetOrganisationsForAutoValidationAsync()
    {
      var organisations = await _dataContext.Organisation.Where(
                          org => !org.IsDeleted && org.CreatedOnUtc >= TimeZoneInfo.ConvertTimeToUtc(startDate) && org.CreatedOnUtc <= TimeZoneInfo.ConvertTimeToUtc(endDate)
                          && !_dataContext.OrganisationAudit.Any(orgAudit => orgAudit.OrganisationId == org.Id))
                          .Select(o => new OrganisationDetail()
                          {
                            Id = o.Id,
                            CiiOrganisationId = o.CiiOrganisationId,
                            SupplierBuyerType = o.SupplierBuyerType.Value,
                            LegalName = o.LegalName,
                            RightToBuy = o.RightToBuy,
                            CreatedOnUtc = o.CreatedOnUtc
                          }).ToListAsync();

      return organisations;
    }
  }
}
