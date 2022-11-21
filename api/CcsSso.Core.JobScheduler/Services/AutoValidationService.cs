using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CcsSso.Core.JobScheduler.Services
{
  public class AutoValidationService : IAutoValidationService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AutoValidationService> _logger;

    public AutoValidationService(IHttpClientFactory httpClientFactory, AppSettings appSettings, ILogger<AutoValidationService> logger)
    {
      _httpClientFactory = httpClientFactory;
      _appSettings = appSettings;
      _logger = logger;

    }


    public async Task PerformJobAsync(List<OrganisationDetail> organisations)
    {
      if (organisations != null)
      {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.WrapperApiSettings.ApiKey);
        client.BaseAddress = new Uri(_appSettings.WrapperApiSettings.Url);

        foreach (var orgDetail in organisations)
        {
          try
          {
            Console.WriteLine($"Autovalidation CiiOrganisationId:- {orgDetail.CiiOrganisationId} ");

            var url = "/organisations/" + orgDetail.CiiOrganisationId + "/autovalidationjob";
            var response = await client.PostAsync(url, null);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
              Console.WriteLine($"Org autovalidation success " + orgDetail.CiiOrganisationId);
            }
            else
            {
              Console.WriteLine($"Org autovalidation falied " + orgDetail.CiiOrganisationId);
            }
          }
          catch (Exception e)
          {
            Console.WriteLine($"Org autovalidation error " + JsonConvert.SerializeObject(e));
          }
        }
      }
    }
  }
}
