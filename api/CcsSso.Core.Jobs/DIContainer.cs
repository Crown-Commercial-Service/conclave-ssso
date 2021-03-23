using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.Jobs.Contracts;
using CcsSso.Core.Jobs.Services;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Core.Jobs
{
  public class DIContainer
  {
    private ServiceProvider _serviceProvider;

    public async Task RegisterDependenciesAsync()
    {
      var collection = new ServiceCollection();

      var secrets = await LoadSecretsAsync();
      var appSettings = ConfigurationManager.AppSettings;
      var dbConnection = secrets["DbConnection"].ToString();
      var CiiSettings = JsonConvert.DeserializeObject<CiiSettings>(secrets["CIISettings"].ToString());
      collection.AddSingleton(s =>
      {
        int.TryParse(appSettings["OrganizationRegistrationExpiredThresholdInMinutes"], out int organizationRegistrationExpiredThresholdInMinutes);

        if (organizationRegistrationExpiredThresholdInMinutes == 0)
        {
          organizationRegistrationExpiredThresholdInMinutes = 60 * 36; // 36 hours as default
        }

        return new AppSettings()
        {
          DbConnection = dbConnection,
          SecurityApiSettings = new SecurityApiSettings()
          {
            ApiKey = secrets["SecurityApiSettings.ApiKey"].ToString(),
            Url = secrets["SecurityApiSettings.Url"].ToString()
          },
          ScheduleJobSettings = new ScheduleJobSettings()
          {
            OrganizationRegistrationExpiredThresholdInMinutes = organizationRegistrationExpiredThresholdInMinutes
          },
          CiiSettings = new CiiSettings()
          {
            ApiKey = CiiSettings.ApiKey,
            BaseURL = CiiSettings.BaseURL
          }
        };
      });

      collection.AddSingleton<IJobServiceManager, JobServiceManager>();
      collection.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() =>
      {
        return new HttpClientHandler()
        {
          AllowAutoRedirect = true,
          UseDefaultCredentials = true
        };
      });
      collection.AddSingleton<IDataTimeService, DataTimeService>();
      collection.AddDbContext<DataContext>(options => options.UseNpgsql(dbConnection));
      collection.AddScoped<IDataContext>(s => s.GetRequiredService<DataContext>());
      collection.AddSingleton<OrganisationDeleteForInactiveRegistrationJob>();

      collection.AddScoped<IScheduledJobServiceFactory>(s => new ScheduledJobServiceFactory(new List<IJob>()
      {
        s.GetRequiredService<OrganisationDeleteForInactiveRegistrationJob>(),
      }));

      _serviceProvider = collection.BuildServiceProvider();
      collection.AddSingleton<IServiceProvider>(_serviceProvider);
    }

    public async Task RegisterStatupJobsAsync()
    {
      var startUpService = _serviceProvider.GetService<IJobServiceManager>();
      await startUpService.PerformJobAsync();
    }

    public void ClearResoruces()
    {
      _serviceProvider.Dispose();
    }

    private async Task<Dictionary<string, object>> LoadSecretsAsync()
    {
      var env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var vault = (JObject)JsonConvert.DeserializeObject<JObject>(env)["hashicorp-vault"][0];
      var vcapSettings = JsonConvert.DeserializeObject<VCapSettings>(vault.ToString());

      IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken: vcapSettings.credentials.auth.token);
      var vaultClientSettings = new VaultClientSettings(vcapSettings.credentials.address, authMethod)
      {
        ContinueAsyncTasksOnCapturedContext = false
      };
      var client = new VaultClient(vaultClientSettings);
      var _secrets = await client.V1.Secrets.Cubbyhole.ReadSecretAsync(secretPath: "brickendon");
      return _secrets.Data;
    }
  }
}
