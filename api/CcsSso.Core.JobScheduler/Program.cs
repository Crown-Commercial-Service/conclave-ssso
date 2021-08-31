using CcsSso.Core.Domain.Jobs;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Core.JobScheduler
{
  public class Program
  {
    private static bool vaultEnabled;
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((hostingContext, config) =>
        {
          var configBuilder = new ConfigurationBuilder()
                         .AddJsonFile("appsettings.json", optional: false)
                         .Build();
          var builtConfig = config.Build();
          vaultEnabled = configBuilder.GetValue<bool>("VaultEnabled");
          if (!vaultEnabled)
          {
            config.AddJsonFile("appsecrets.json", optional: false, reloadOnChange: true);
          }
        }).ConfigureServices((hostContext, services) =>
        {
          string dbConnection;
          CiiSettings ciiSettings;
          SecurityApiSettings securityApiSettings;
          ScheduleJobSettingsVault scheduleJobSettingsVault;
          if (vaultEnabled)
          {
            var secrets = LoadSecretsAsync().Result;
            dbConnection = secrets["DbConnection"].ToString();
            ciiSettings = JsonConvert.DeserializeObject<CiiSettings>(secrets["CIISettings"].ToString());
            securityApiSettings = JsonConvert.DeserializeObject<SecurityApiSettings>(secrets["SecurityApiSettings"].ToString());
            scheduleJobSettingsVault = JsonConvert.DeserializeObject<ScheduleJobSettingsVault>(secrets["ScheduleJobSettings"].ToString());
          }
          else
          {
            var config = hostContext.Configuration;
            dbConnection = config["DbConnection"];
            ciiSettings = config.GetSection("CIISettings").Get<CiiSettings>();
            securityApiSettings = config.GetSection("SecurityApiSettings").Get<SecurityApiSettings>();
            scheduleJobSettingsVault = config.GetSection("ScheduleJobSettings").Get<ScheduleJobSettingsVault>();
          }

          services.AddSingleton(s =>
          {
            int.TryParse(scheduleJobSettingsVault.OrganizationRegistrationExpiredThresholdInMinutes, out int organizationRegistrationExpiredThresholdInMinutes);
            int.TryParse(scheduleJobSettingsVault.JobSchedulerExecutionFrequencyInMinutes, out int jobSchedulerExecutionFrequencyInMinutes);

            if (organizationRegistrationExpiredThresholdInMinutes == 0)
            {
              organizationRegistrationExpiredThresholdInMinutes = 60 * 36; // 36 hours as default
            }

            if (jobSchedulerExecutionFrequencyInMinutes == 0)
            {
              jobSchedulerExecutionFrequencyInMinutes = 10;
            }

            return new AppSettings()
            {
              DbConnection = dbConnection,
              SecurityApiSettings = new SecurityApiSettings()
              {
                ApiKey = securityApiSettings.ApiKey,
                Url = securityApiSettings.Url
              },
              ScheduleJobSettings = new ScheduleJobSettings()
              {
                OrganizationRegistrationExpiredThresholdInMinutes = organizationRegistrationExpiredThresholdInMinutes,
                JobSchedulerExecutionFrequencyInMinutes = jobSchedulerExecutionFrequencyInMinutes
              },
              CiiSettings = new CiiSettings()
              {
                Token = ciiSettings.Token,
                Url = ciiSettings.Url
              }
            };
          });

          services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() =>
          {
            return new HttpClientHandler()
            {
              AllowAutoRedirect = true,
              UseDefaultCredentials = true
            };
          });
          services.AddSingleton<IDateTimeService, DateTimeService>();
          services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(dbConnection));
          services.AddHostedService<OrganisationDeleteForInactiveRegistrationJob>();
          services.AddSingleton<RequestContext>(s => new RequestContext { UserId = 0 });
        });

    private static async Task<Dictionary<string, object>> LoadSecretsAsync()
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
      var mountPathValue = vcapSettings.credentials.backends_shared.space.Split("/secret").FirstOrDefault();
      var _secrets = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/org-dereg-job", mountPathValue);
      return _secrets.Data;
    }
  }
}
