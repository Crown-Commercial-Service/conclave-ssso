using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Services;
using CcsSso.Core.Service;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
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
          List<UserDeleteJobSetting> userDeleteJobSettings;
          SecurityApiSettings securityApiSettings;
          ScheduleJobSettings scheduleJobSettings;
          RedisCacheSettingsVault redisCacheSettingsVault;
          EmailConfigurationInfo emailConfigurationInfo;
          if (vaultEnabled)
          {
            var secrets = LoadSecretsAsync().Result;
            dbConnection = secrets["DbConnection"].ToString();
            ciiSettings = JsonConvert.DeserializeObject<CiiSettings>(secrets["CIISettings"].ToString());
            userDeleteJobSettings = JsonConvert.DeserializeObject<List<UserDeleteJobSetting>>(secrets["UserDeleteJobSettings"].ToString());
            emailConfigurationInfo = JsonConvert.DeserializeObject<EmailConfigurationInfo>(secrets["Email"].ToString());
            securityApiSettings = JsonConvert.DeserializeObject<SecurityApiSettings>(secrets["SecurityApiSettings"].ToString());
            scheduleJobSettings = JsonConvert.DeserializeObject<ScheduleJobSettings>(secrets["ScheduleJobSettings"].ToString());
            redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(secrets["RedisCacheSettings"].ToString());
          }
          else
          {
            var config = hostContext.Configuration;
            dbConnection = config["DbConnection"];
            ciiSettings = config.GetSection("CIISettings").Get<CiiSettings>();
            userDeleteJobSettings = config.GetSection("UserDeleteJobSettings").Get<List<UserDeleteJobSetting>>();
            securityApiSettings = config.GetSection("SecurityApiSettings").Get<SecurityApiSettings>();
            scheduleJobSettings = config.GetSection("ScheduleJobSettings").Get<ScheduleJobSettings>();
            emailConfigurationInfo = config.GetSection("Email").Get<EmailConfigurationInfo>();
            redisCacheSettingsVault = config.GetSection("RedisCacheSettings").Get<RedisCacheSettingsVault>();
          }

          services.AddSingleton(s =>
          {
            return new AppSettings()
            {
              DbConnection = dbConnection,
              UserDeleteJobSettings = userDeleteJobSettings,
              SecurityApiSettings = new SecurityApiSettings()
              {
                ApiKey = securityApiSettings.ApiKey,
                Url = securityApiSettings.Url
              },
              ScheduleJobSettings = scheduleJobSettings,
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
          services.AddSingleton(s =>
          {
            return emailConfigurationInfo;
          });

          services.AddSingleton<IDateTimeService, DateTimeService>();
          services.AddSingleton<IIdamSupportService, IdamSupportService>();
          services.AddScoped<IOrganisationSupportService, OrganisationSupportService>(); 
          services.AddScoped<IContactSupportService, ContactSupportService>(); 
          services.AddSingleton<IEmailSupportService, EmailSupportService>();
          services.AddSingleton<IEmailProviderService, EmailProviderService>();
          services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(dbConnection));
          services.AddHostedService<OrganisationDeleteForInactiveRegistrationJob>();
          services.AddHostedService<UnverifiedUserDeleteJob>();
          services.AddSingleton<RequestContext>(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
          services.AddSingleton<IRemoteCacheService, RedisCacheService>();
          services.AddSingleton<ICacheInvalidateService, CacheInvalidateService>();
          services.AddSingleton<RedisConnectionPoolService>(_ =>
            new RedisConnectionPoolService(redisCacheSettingsVault.ConnectionString)
          );
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
