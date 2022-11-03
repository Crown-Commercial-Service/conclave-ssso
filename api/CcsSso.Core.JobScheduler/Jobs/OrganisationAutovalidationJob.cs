using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Enum;
using CcsSso.Core.JobScheduler.Services;
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
  public class OrganisationAutovalidationJob : BackgroundService
  {
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheInvalidateService _cacheInvalidateService;
    private readonly IIdamSupportService _idamSupportService;

    public OrganisationAutovalidationJob(IServiceScopeFactory factory, IDateTimeService dataTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory, ICacheInvalidateService cacheInvalidateService,
      IIdamSupportService idamSupportService)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _appSettings = appSettings;
      _httpClientFactory = httpClientFactory;
      _cacheInvalidateService = cacheInvalidateService;
      _idamSupportService = idamSupportService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

      while (!stoppingToken.IsCancellationRequested)
      {
        Console.WriteLine($" ****************Organization autovalidation batch processing job started ***********");
        await PerformJobAsync();
        //TODO: Need to run this only once
        await Task.Delay(_appSettings.ScheduleJobSettings.OrganisationAutovalidationJobExecutionFrequencyInMinutes * 60000, stoppingToken);
        Console.WriteLine($"******************Organization autovalidation batch processing job ended ***********");
      }
    }

    private async Task PerformJobAsync()
    {
      var organisations = await GetOrganisationsAsync();
      
      Console.WriteLine($"Autovalidation {organisations.Count()} organizations found");

      if (organisations != null)
      {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.WrapperApiSettings.ApiKey);
        client.BaseAddress = new Uri(_appSettings.WrapperApiSettings.Url);

        foreach (var orgDetail in organisations)
        {
          try
          {
            Console.WriteLine($"Autovalidation CiiOrganisationId:- {orgDetail.Item1} ");

            var url = "/organisations/" + orgDetail.Item1 + "/autovalidationjob";
            var response = await client.PostAsync(url, null);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
              Console.WriteLine($"Org autovalidation success " + orgDetail.Item1);
            }
            else
            {
              Console.WriteLine($"Org autovalidation falied " + orgDetail.Item1);
            }
          }
          catch (Exception e)
          {
            Console.WriteLine($"Org autovalidation error " + JsonConvert.SerializeObject(e));
          }
        }
      }
    }

    public async Task<List<Tuple<string, int>>> GetOrganisationsAsync()
    {
      //TODO: Add condition to exclude orgs where autovalidation already performed 

      var organisations = await _dataContext.Organisation.Where(
                          org => !org.IsActivated && !org.IsDeleted && org.SupplierBuyerType > 0)
                          .Select(o => new Tuple<string, int>(o.CiiOrganisationId, o.SupplierBuyerType.Value)).ToListAsync();

      return organisations;
    }    
  }
}
