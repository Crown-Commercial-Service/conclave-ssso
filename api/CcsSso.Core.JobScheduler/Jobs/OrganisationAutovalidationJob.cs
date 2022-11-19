using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Enum;
using CcsSso.Core.JobScheduler.Model;
using CcsSso.Core.JobScheduler.Services;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler
{
  public class OrganisationAutovalidationJob : BackgroundService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheInvalidateService _cacheInvalidateService;
    private readonly IIdamSupportService _idamSupportService;
    private readonly IAutoValidationService _autoValidationService;
    private readonly ILogger<OrganisationAutovalidationJob> _logger;
    private bool enable;
    private bool ranOnce;
    private bool reportingMode;
    private DateTime startDate;
    private DateTime endDate;

    public OrganisationAutovalidationJob(ILogger<OrganisationAutovalidationJob> logger, IServiceScopeFactory factory, IDateTimeService dataTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory, ICacheInvalidateService cacheInvalidateService,
      IIdamSupportService idamSupportService,IAutoValidationService autoValidationService)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _appSettings = appSettings;
      _httpClientFactory = httpClientFactory;
      _cacheInvalidateService = cacheInvalidateService;
      _idamSupportService = idamSupportService;
      _autoValidationService = autoValidationService;
      _logger = logger;
      ranOnce = false;
      enable = false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

      while (!stoppingToken.IsCancellationRequested)
      {
        reportingMode = _appSettings.OrgAutoValidationOneTimeJob.ReportingMode;
        int interval = _appSettings.ScheduleJobSettings.OrganisationAutovalidationJobExecutionFrequencyInMinutes * 60000;

        var dates =await ReadDateRange(interval, stoppingToken);

        if (dates == null)
        {
          await Task.Delay(interval, stoppingToken);
          continue;
        }

        startDate = dates.Item1;
        endDate = dates.Item2;

        Console.WriteLine($" ****************Organization autovalidation batch processing job started ***********");
        await PerformJobAsync();
        //TODO: Need to run this only once
        await Task.Delay(interval, stoppingToken);
        Console.WriteLine($"******************Organization autovalidation batch processing job ended ***********");
      }
    }

    private async Task PerformJobAsync()
    {
      var organisations = await GetOrganisationsAsync();

      Console.WriteLine($"Autovalidation Total Number of organisation found : {organisations.Count()}");
      await _autoValidationService.PerformJobAsync(organisations);

      await _autoValidationService.PerformJobAsync(organisations);


    }

    private async Task<Tuple<DateTime, DateTime>> ReadDateRange(int interval,CancellationToken stoppingToken)
    {
      var startDateString = _appSettings.OrgAutoValidationOneTimeJob.StartDate;
      var endDateString = _appSettings.OrgAutoValidationOneTimeJob.EndDate;

      if (startDateString == null || endDateString == null)
      {
        _logger.LogError("One time validation needs start and end date. Skipping this iteration.");
        // await Task.Delay(interval, stoppingToken);
        return null;
      }

      try
      {
        startDate = DateTime.ParseExact(startDateString, "yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(endDateString, "yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture);

      }
      catch (FormatException)
      {
        _logger.LogError("{0} or {1} is not in the correct format. Date format should be as follows 'yyyy-MM-dd' Skipping this iteration.", startDateString, endDateString);
        // await Task.Delay(interval, stoppingToken);
        return null;

      }
      catch (Exception)
      {
        _logger.LogError("Error while reading the start or end date {0}, {1}. Skipping this iteration.", startDateString, endDateString);
        //await Task.Delay(interval, stoppingToken);
        return null;
        
      }
      return new Tuple<DateTime,DateTime>(startDate,endDate);
    }

    public async Task<List<OrganisationDetail>> GetOrganisationsAsync()
    {
      //TODO: Add condition to exclude orgs where autovalidation already performed 

      var organisations = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted && org.CreatedOnUtc >= TimeZoneInfo.ConvertTimeToUtc(startDate) && org.CreatedOnUtc <= TimeZoneInfo.ConvertTimeToUtc(endDate))
                          .Select(o => new OrganisationDetail()
                          {
                            Id = o.Id,
                            CiiOrganisationId = o.CiiOrganisationId,
                            SupplierBuyerType= o.SupplierBuyerType.Value,
                            LegalName =o.LegalName,
                            RightToBuy=o.RightToBuy,
                            CreatedOnUtc = o.CreatedOnUtc
                          }).ToListAsync();

      return organisations;
    }
  }
}
