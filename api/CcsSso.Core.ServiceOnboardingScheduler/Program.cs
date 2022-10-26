
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.ServiceOnboardingScheduler.Jobs;
using CcsSso.Core.ServiceOnboardingScheduler.Model;
using System.Reflection.Metadata;

namespace CcsSso.Core.ServiceOnboardingScheduler
  {
  public class Program
  {
    private static bool vaultEnabled;
    private static string vaultSource;

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
            vaultSource = configBuilder.GetValue<string>("Source");
            if (!vaultEnabled)
            {
              config.AddJsonFile("appsecrets.json", optional: false, reloadOnChange: true);
            }
          })
          .ConfigureServices((hostContext, services) =>
          {
            OnBoardingAppSettings appSettings = GetConfigurationDetails(hostContext);

            services.AddSingleton(s => appSettings);

            ConfigureHttpClients(services, appSettings);


            //services.AddSingleton<RequestContext>(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
            //services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(appSettings.DbConnection));

            services.AddHostedService<CASOnboardingJob>();
            
          });
    }

    private static void ConfigureHttpClients(IServiceCollection services, OnBoardingAppSettings appSettings)
    {
      services.AddHttpClient("WrapperApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.WrapperApiSettings.Url);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.WrapperApiSettings.ApiKey);
      });
      services.AddHttpClient("ScurityApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.SecurityApiSettings.Url);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.SecurityApiSettings.ApiKey);
      });
      services.AddHttpClient("LookupApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.LookupApiSettings.Url);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.LookupApiSettings.ApiKey);
      });
    }

    private static OnBoardingAppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      ApiSettings SecurityApi, WrapperApi;
      ScheduleJob ScheduleJob;
      OnBoardingDataDuration OnBoardingDataDuration;

      string dbConnection;
      string maxNumbeOfRecordInAReport;

      
        var config = hostContext.Configuration;
        dbConnection = config["DbConnection"];
        SecurityApi = config.GetSection("SecurityApi").Get<ApiSettings>();
        WrapperApi = config.GetSection("WrapperApi").Get<ApiSettings>();
        ScheduleJob = config.GetSection("ScheduleJob").Get<ScheduleJob>();
        OnBoardingDataDuration = config.GetSection("OnBoardingDataDuration").Get<OnBoardingDataDuration>();
        maxNumbeOfRecordInAReport = config["MaxNumbeOfRecordInAReport"].ToString();
      

      var appSettings = new OnBoardingAppSettings()
      {
        DbConnection = dbConnection,
        OnBoardingDataDuration = OnBoardingDataDuration,
        ScheduleJobSettings = ScheduleJob,
        SecurityApiSettings = SecurityApi,
        WrapperApiSettings = WrapperApi,
        MaxNumbeOfRecordInAReport = int.Parse(maxNumbeOfRecordInAReport),
        
      };
      return appSettings;
    }

    
  }
}

