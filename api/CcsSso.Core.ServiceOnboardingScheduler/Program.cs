using CcsSso.Core.ServiceOnboardingScheduler.Jobs;
using CcsSso.Core.ServiceOnboardingScheduler.Model;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain;

namespace CcsSso.Core.ServiceOnboardingScheduler
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
            var configBuilder = new ConfigurationBuilder()
                           .AddJsonFile("appsettings.json", optional: false)
                           .Build();
            var builtConfig = config.Build();
            vaultEnabled = configBuilder.GetValue<bool>("VaultEnabled");

            if (!vaultEnabled)
            {
              config.AddJsonFile("appsecrets.json", optional: false, reloadOnChange: true);
            }
          })

          .ConfigureServices((hostContext, services) =>
          {
            OnBoardingAppSettings appSettings = GetConfigurationDetails(hostContext);

            var config = hostContext.Configuration;
            EmailConfigurationInfo emailConfigurationInfo = config.GetSection("Email").Get<EmailConfigurationInfo>();

            services.AddSingleton(s =>
            {
              return emailConfigurationInfo;
            });

            services.AddSingleton(s => appSettings);

            ConfigureHttpClients(services, appSettings);

            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddSingleton<IEmailProviderService, EmailProviderService>();

            services.AddSingleton(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
            services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(appSettings.DbConnection));

            services.AddHostedService<CASOnboardingJob>();
            
          });
    }

    private static void ConfigureHttpClients(IServiceCollection services, OnBoardingAppSettings appSettings)
    {
      //services.AddHttpClient("WrapperApi", c =>
      //{
      //  c.BaseAddress = new Uri(appSettings.WrapperApiSettings.Url);
      //  c.DefaultRequestHeaders.Add("X-API-Key", appSettings.WrapperApiSettings.ApiKey);
      //});
      //services.AddHttpClient("ScurityApi", c =>
      //{
      //  c.BaseAddress = new Uri(appSettings.SecurityApiSettings.Url);
      //  c.DefaultRequestHeaders.Add("X-API-Key", appSettings.SecurityApiSettings.ApiKey);
      //});
      services.AddHttpClient("LookupApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.LookupApiSettings.Url);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.LookupApiSettings.ApiKey);
      });
    }

    private static OnBoardingAppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      ApiSettings SecurityApi, WrapperApi, LookupApi;
      ScheduleJob ScheduleJob;
      OnBoardingDataDuration OnBoardingDataDuration;

      string dbConnection;
      string maxNumbeOfRecordInAReport;

      
        var config = hostContext.Configuration;
        dbConnection = config["DbConnection"];
        SecurityApi = config.GetSection("SecurityApi").Get<ApiSettings>();
        WrapperApi = config.GetSection("WrapperApi").Get<ApiSettings>();
      LookupApi = config.GetSection("LookupApi").Get<ApiSettings>();

      ScheduleJob = config.GetSection("ScheduleJob").Get<ScheduleJob>();
        OnBoardingDataDuration = config.GetSection("OnBoardingDataDuration").Get<OnBoardingDataDuration>();
        maxNumbeOfRecordInAReport = config["MaxNumbeOfRecordInAReport"].ToString();
      

      var appSettings = new OnBoardingAppSettings()
      {
        DbConnection = dbConnection,
        OnBoardingDataDuration = OnBoardingDataDuration,
        ScheduleJobSettings = ScheduleJob,
        SecurityApiSettings = SecurityApi,
        LookupApiSettings = LookupApi,
        WrapperApiSettings = WrapperApi,
        MaxNumbeOfRecordInAReport = int.Parse(maxNumbeOfRecordInAReport),
        
      };
      return appSettings;
    }

    
  }
}

