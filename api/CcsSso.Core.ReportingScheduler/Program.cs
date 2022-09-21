
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.ReportingScheduler.Jobs;
using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain.Helpers;
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
    private static string vaultSource;
    private static string path = "/conclave-sso/reporting-job/";
    private static IAwsParameterStoreService _awsParameterStoreService;

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
            AppSettings appSettings = GetConfigurationDetails(hostContext);

            services.AddSingleton(s => appSettings);

            ConfigureHttpClients(services, appSettings);

            services.AddSingleton(s =>
            {

              return new S3Configuration
              {
                AccessKeyId = appSettings.S3Configuration.AccessKeyId,
                AccessSecretKey = appSettings.S3Configuration.AccessSecretKey,
                BucketName = appSettings.S3Configuration.BucketName
              };
            });

            services.AddSingleton(s =>
            {

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
            services.AddHostedService<UserReportingJob>();
            services.AddHostedService<ContactReportingJob>();
            services.AddHostedService<AuditReportingJob>();
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
      string writeCSVDataInLog;

      if (vaultEnabled)
      {
        if (vaultSource == "AWS")
        {
          var parameters = LoadAwsSecretsAsync().Result;

          var dbName = _awsParameterStoreService.FindParameterByName(parameters, path + "DbName");
          var dbConnectionEndPoint = _awsParameterStoreService.FindParameterByName(parameters, path + "DbConnection");

          if (!string.IsNullOrEmpty(dbName))
          {
            dbConnection = UtilityHelper.GetDatbaseConnectionString(dbName, dbConnectionEndPoint);
          }
          else
          {
            dbConnection = dbConnectionEndPoint;
          }

          SecurityApi = (ApiConfig)FillAwsParamsValue(typeof(ApiConfig), parameters, "SecurityApi");
          WrapperApi = (ApiConfig)FillAwsParamsValue(typeof(ApiConfig), parameters, "WrapperApi");
          ScheduleJob = (ScheduleJob)FillAwsParamsValue(typeof(ScheduleJob), parameters);
          ReportDataDurations = (ReportDataDuration)FillAwsParamsValue(typeof(ReportDataDuration), parameters);
          S3Configuration = (S3Configuration)FillAwsParamsValue(typeof(S3Configuration), parameters);
          azureBlobConfiguration = (AzureBlobConfiguration)FillAwsParamsValue(typeof(AzureBlobConfiguration), parameters);
          maxNumbeOfRecordInAReport =  _awsParameterStoreService.FindParameterByName(parameters, path + "MaxNumbeOfRecordInAReport");
          writeCSVDataInLog = _awsParameterStoreService.FindParameterByName(parameters, path + "WriteCSVDataInLog");

          Console.WriteLine("Aws Param name: DbConnection value:" + dbConnection);
          Console.WriteLine("Aws Param name: SecurityApi value:" + JsonConvert.SerializeObject(SecurityApi));
          Console.WriteLine("Aws Param name: WrapperApi value:" + JsonConvert.SerializeObject(WrapperApi));
          Console.WriteLine("Aws Param name: ScheduleJob value:" + JsonConvert.SerializeObject(ScheduleJob));
          Console.WriteLine("Aws Param name: ReportDataDurations value:" + JsonConvert.SerializeObject(ReportDataDurations));
          Console.WriteLine("Aws Param name: S3Configuration value:" + JsonConvert.SerializeObject(S3Configuration));
          Console.WriteLine("Aws Param name: azureBlobConfiguration value:" + JsonConvert.SerializeObject(azureBlobConfiguration));
          Console.WriteLine("Aws Param name: maxNumbeOfRecordInAReport value:" + JsonConvert.SerializeObject(maxNumbeOfRecordInAReport));
          Console.WriteLine("Aws Param name: writeCSVDataInLog value:" + JsonConvert.SerializeObject(writeCSVDataInLog));
        }
        else
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
          writeCSVDataInLog = secrets["WriteCSVDataInLog"].ToString();

          Console.WriteLine("Harsicorp Param name: DbConnection value:" + dbConnection);
          Console.WriteLine("Harsicorp Param name: SecurityApi value:" + JsonConvert.SerializeObject(SecurityApi));
          Console.WriteLine("Harsicorp Param name: WrapperApi value:" + JsonConvert.SerializeObject(WrapperApi));
          Console.WriteLine("Harsicorp Param name: ScheduleJob value:" + JsonConvert.SerializeObject(ScheduleJob));
          Console.WriteLine("Harsicorp Param name: ReportDataDurations value:" + JsonConvert.SerializeObject(ReportDataDurations));
          Console.WriteLine("Harsicorp Param name: S3Configuration value:" + JsonConvert.SerializeObject(S3Configuration));
          Console.WriteLine("Harsicorp Param name: azureBlobConfiguration value:" + JsonConvert.SerializeObject(azureBlobConfiguration));
          Console.WriteLine("Harsicorp Param name: maxNumbeOfRecordInAReport value:" + JsonConvert.SerializeObject(maxNumbeOfRecordInAReport));
          Console.WriteLine("Harsicorp Param name: writeCSVDataInLog value:" + JsonConvert.SerializeObject(writeCSVDataInLog));
        }
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
        writeCSVDataInLog = config["WriteCSVDataInLog"].ToString();
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
        MaxNumbeOfRecordInAReport = int.Parse(maxNumbeOfRecordInAReport),
        WriteCSVDataInLog = bool.Parse(writeCSVDataInLog)
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

    private static async Task<List<Parameter>> LoadAwsSecretsAsync()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
      return await _awsParameterStoreService.GetParameters(path);
    }

    private static dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters, string apiConfig = "")
    {
      if (objType  == typeof(ApiConfig))
      {
        if (!string.IsNullOrWhiteSpace(apiConfig))
        {
          return new ApiConfig()
          {
            Url = _awsParameterStoreService.FindParameterByName(parameters, path + apiConfig + "/Url"),
            ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + apiConfig + "/ApiKey"),
          };
        }
        else
        {
          return null;
        }
      }
      else if (objType  == typeof(ScheduleJob))
      {
        return new ScheduleJob()
        {
          AuditLogReportingJobScheduleInMinutes   = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJob/AuditLogReportingJobScheduleInMinutes")),
          ContactReportingJobScheduleInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJob/ContactReportingJobScheduleInMinutes")),
          OrganisationReportingJobScheduleInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJob/OrganisationReportingJobScheduleInMinutes")),
          UserReportingJobScheduleInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJob/UserReportingJobScheduleInMinutes"))
        };
      }
      else if (objType  == typeof(ReportDataDuration))
      {
        return new ReportDataDuration()
        {
          AuditLogReportingDurationInMinutes =Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ReportDataDuration/AuditLogReportingDurationInMinutes")),
          ContactReportingDurationInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ReportDataDuration/ContactReportingDurationInMinutes")),
          OrganisationReportingDurationInMinutes =Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ReportDataDuration/OrganisationReportingDurationInMinutes")),
          UserReportingDurationInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ReportDataDuration/UserReportingDurationInMinutes"))
        };
      }
      else if (objType  == typeof(S3Configuration))
      {
        var s3Name = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/Name"); 

        if (!string.IsNullOrEmpty(s3Name))
        {
          var s3Settings = UtilityHelper.GetS3Settings(s3Name);

          return new S3Configuration()
          {
            AccessKeyId = s3Settings?.credentials?.aws_access_key_id,
            AccessSecretKey = s3Settings?.credentials?.aws_secret_access_key,
            BucketName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/BucketName"),
            ServiceUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/ServiceUrl")
          };
        }
        else
        {
          return new S3Configuration()
          {
            AccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/AccessKeyId"),
            AccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/AccessSecretKey"),
            BucketName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/BucketName"),
            ServiceUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "S3Configuration/ServiceUrl")
          };
        }
      }
      else if (objType  == typeof(AzureBlobConfiguration))
      {
        return new AzureBlobConfiguration()
        {
          AccountKey = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/AccountKey"),
          AccountName = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/AccountName"),
          AzureBlobContainer = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/AzureBlobContainer"),
          EndpointAzure = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/EndpointAzure"),
          EndpointProtocol = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/EndpointProtocol"),
          FileExtension = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/FileExtension"),
          Fileheader = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/Fileheader"),
          FilePathPrefix = _awsParameterStoreService.FindParameterByName(parameters, path + "AzureBlobConfiguration/FilePathPrefix")
        };
      }
      return null;
    }
  }
}
