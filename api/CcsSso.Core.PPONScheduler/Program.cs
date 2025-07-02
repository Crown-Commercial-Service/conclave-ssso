using CcsSso.Core.PPONScheduler.Jobs;
using CcsSso.Core.PPONScheduler.Model;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Core.PPONScheduler.Service.Contracts;
using CcsSso.Core.PPONScheduler.Service;
using CcsSso.Core.Service;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Service.Wrapper;
using CcsSso.Core.Domain.Contracts.Wrapper;

namespace CcsSso.Core.PPONScheduler
{
    public class Program
  {
    private static bool vaultEnabled;

    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
      return Host.CreateDefaultBuilder(args)
      .ConfigureAppConfiguration((hostingContext, config) =>
      {
        PopulateAppConfiguration(config);
      })

      .ConfigureServices((hostContext, services) =>
      {
        PPONAppSettings appSettings = GetConfigurationDetails(hostContext);
        ConfigureHttpClients(services, appSettings);
        ConfigureModels(services, appSettings);
        ConfigureServices(services, appSettings);
        ConfigureJobs(services);
      });
    }

    private static void PopulateAppConfiguration(IConfigurationBuilder config)
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
    }

    private static void ConfigureModels(IServiceCollection services, PPONAppSettings appSettings)
    {
      services.AddSingleton(s =>
      {
        Dtos.Domain.Models.CiiConfig ciiConfigInfo = new Dtos.Domain.Models.CiiConfig()
        {
          url = appSettings.CiiSettings.Url,
          token = appSettings.CiiSettings.Token
        };
        return ciiConfigInfo;
      });
		}

    private static void ConfigureJobs(IServiceCollection services)
    {
      services.AddHostedService<OneTimePPONJob>();
      services.AddHostedService<PPONJob>();
    }

    private static void ConfigureServices(IServiceCollection services, PPONAppSettings appSettings)
    {
      services.AddSingleton(s => appSettings);
      
      
      services.AddScoped<IDateTimeService, DateTimeService>();
      services.AddScoped<IPPONService, PPONService>();
      services.AddScoped<IWrapperOrganisationService, WrapperOrganisationService>();
      services.AddScoped<IWrapperApiService, WrapperApiService>(); 
    }

    private static void ConfigureHttpClients(IServiceCollection services, PPONAppSettings appSettings)
    {
      services.AddHttpClient("PPONApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.PPONApiSettings.Url);
        c.DefaultRequestHeaders.Add("x-api-key", appSettings.PPONApiSettings.Key);
      });
      services.AddHttpClient("CiiApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.CiiSettings.Url);
        c.DefaultRequestHeaders.Add("x-api-key", appSettings.CiiSettings.Token);
      });
			services.AddHttpClient("OrgWrapperApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.IsApiGatewayEnabled ? appSettings.WrapperApiSettings.ApiGatewayEnabledOrgUrl : appSettings.WrapperApiSettings.ApiGatewayDisabledOrgUrl);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.WrapperApiSettings.OrgApiKey);
      });
    }

    private static PPONAppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      ScheduleJob scheduleJob;
      OneTimeJob oneTimeJob;
      CiiSettings ciiSettings;
      ApiSettings pPONApiSettings;
      WrapperApiSettings wrapperApiSettings;

      string dbConnection;

      var config = hostContext.Configuration;

      bool.TryParse(config["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);

      dbConnection = config["DbConnection"];

      ciiSettings = config.GetSection("CIIApi").Get<CiiSettings>();
      pPONApiSettings = config.GetSection("PPONApi").Get<ApiSettings>();

      scheduleJob = config.GetSection("ScheduleJob").Get<ScheduleJob>();
      oneTimeJob = config.GetSection("OneTimeJob").Get<OneTimeJob>();

      wrapperApiSettings = config.GetSection("WrapperApiSettings").Get<WrapperApiSettings>();

      var appSettings = new PPONAppSettings()
      {
        IsApiGatewayEnabled = isApiGatewayEnabled,
        DbConnection = dbConnection,
        CiiSettings = ciiSettings,
        PPONApiSettings = pPONApiSettings,
        ScheduleJobSettings = scheduleJob,
        OneTimeJobSettings = oneTimeJob,
        WrapperApiSettings = wrapperApiSettings,
      };

      return appSettings;
    }
  }
}

