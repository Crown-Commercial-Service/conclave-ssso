
using CcsSso.Core.ReportingScheduler.Jobs;
using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using S3Configuration = CcsSso.Core.ReportingScheduler.Models.S3Configuration;

namespace CcsSso.Core.ReportingScheduler
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
            AppSettings appSettings = GetConfigurationDetails(hostContext);

            services.AddSingleton(s => appSettings);

            ConfigureHttpClients(services, appSettings);

            services.AddSingleton(s => {

              return new S3Configuration
              {
                AccessKeyId = appSettings.S3Configuration.AccessKeyId,
                AccessSecretKey = appSettings.S3Configuration.AccessSecretKey,
                BucketName = appSettings.S3Configuration.BucketName
              };
            });

            services.AddSingleton(s => {

              return new AzureBlobConfiguration
              {
                EndpointProtocol = appSettings.AzureBlobConfiguration.EndpointProtocol,
                AccountName = appSettings.AzureBlobConfiguration.AccountName,
                AccountKey = appSettings.AzureBlobConfiguration.AccountKey,
                EndpointAzure = appSettings.AzureBlobConfiguration.EndpointAzure,
                AzureBlobContainer = appSettings.AzureBlobConfiguration.AzureBlobContainer,
                Fileheader = appSettings.AzureBlobConfiguration.Fileheader,
                FileExtension = appSettings.AzureBlobConfiguration.FileExtension,
                FilePathPrefix = appSettings.AzureBlobConfiguration.FilePathPrefix
              };
            });

            services.AddSingleton<RequestContext>(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
            services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(appSettings.DbConnection));

            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddSingleton<ICSVConverter, CSVConverter>();
            services.AddSingleton<IFileUploadToCloud, FileUploadToCloud>();
            services.AddHostedService<OrganisationReportingJob>();
            //services.AddHostedService<UserReportingJob>();
            //services.AddHostedService<ContactReportingJob>();
            //services.AddHostedService<AuditReportingJob>();
          });
    }

    private static void ConfigureHttpClients(IServiceCollection services, AppSettings appSettings)
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
    }

    private static AppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      ApiConfig SecurityApi, WrapperApi;
      ScheduleJob ScheduleJob;
      ReportDataDuration ReportDataDurations;
      S3Configuration S3Configuration;
      AzureBlobConfiguration azureBlobConfiguration;

      string dbConnection;
      string maxNumbeOfRecordInAReport;

      if (vaultEnabled)
      {
        var secrets = LoadSecretsAsync().Result;
        dbConnection = secrets["DbConnection"].ToString();
        SecurityApi = JsonConvert.DeserializeObject<ApiConfig>(secrets["SecurityApi"].ToString());
        WrapperApi = JsonConvert.DeserializeObject<ApiConfig>(secrets["WrapperApi"].ToString());
        ScheduleJob = JsonConvert.DeserializeObject<ScheduleJob>(secrets["ScheduleJob"].ToString());
        ReportDataDurations = JsonConvert.DeserializeObject<ReportDataDuration>(secrets["ReportDataDuration"].ToString());
        S3Configuration = JsonConvert.DeserializeObject<S3Configuration>(secrets["S3Configuration"].ToString());
        azureBlobConfiguration = JsonConvert.DeserializeObject<AzureBlobConfiguration>(secrets["AzureBlobConfiguration"].ToString());
        maxNumbeOfRecordInAReport = secrets["MaxNumbeOfRecordInAReport"].ToString();
      }
      else
      {
        var config = hostContext.Configuration;
        dbConnection = config["DbConnection"];
        SecurityApi = config.GetSection("SecurityApi").Get<ApiConfig>();
        WrapperApi = config.GetSection("WrapperApi").Get<ApiConfig>();
        ScheduleJob = config.GetSection("ScheduleJob").Get<ScheduleJob>();
        ReportDataDurations = config.GetSection("ReportDataDuration").Get<ReportDataDuration>();
        S3Configuration = config.GetSection("S3Configuration").Get<S3Configuration>();
        azureBlobConfiguration = config.GetSection("AzureBlobConfiguration").Get<AzureBlobConfiguration>();
        maxNumbeOfRecordInAReport = config["MaxNumbeOfRecordInAReport"].ToString();
      }

      var appSettings = new AppSettings()
      {
        DbConnection = dbConnection,
        ReportDataDurations = ReportDataDurations,
        ScheduleJobSettings = ScheduleJob,
        SecurityApiSettings = SecurityApi,
        WrapperApiSettings = WrapperApi,
        S3Configuration =S3Configuration,
        AzureBlobConfiguration = azureBlobConfiguration,
        MaxNumbeOfRecordInAReport = int.Parse(maxNumbeOfRecordInAReport)
      };
      return appSettings;
    }


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
      var _secrets = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/reporting-job", mountPathValue);
      return _secrets.Data;
    }
  }
}
