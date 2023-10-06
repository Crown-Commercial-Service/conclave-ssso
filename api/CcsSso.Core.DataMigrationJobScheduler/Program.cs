using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DataMigrationJobScheduler.Contracts;
using CcsSso.Core.DataMigrationJobScheduler.Services;
using CcsSso.Core.DataMigrationJobScheduler.Jobs;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using CcsSso.Core.DataMigrationJobScheduler.Services;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Service;
using CcsSso.Core.Service.External;
using CcsSso.Core.Service.Wrapper;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using S3ConfigurationInfo = CcsSso.Core.DataMigrationJobScheduler.Model.S3ConfigurationInfo;
using IAwsS3Service = CcsSso.Core.DataMigrationJobScheduler.Contracts.IAwsS3Service;
using AwsS3Service = CcsSso.Core.DataMigrationJobScheduler.Services.AwsS3Service;
using CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts;

using IWrapperApiService= CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts.IWrapperApiService;
using WrapperApiService = CcsSso.Core.DataMigrationJobScheduler.Wrapper.WrapperApiService;
using IWrapperOrganisationService = CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts.IWrapperOrganisationService;
using WrapperOrganisationService = CcsSso.Core.DataMigrationJobScheduler.Wrapper.WrapperOrganisationService;

namespace CcsSso.Core.DataMigrationJobScheduler
{
  public class Program
  {
    private static bool vaultEnabled;
    private static string vaultSource;
    private static string path = "/conclave-sso/delegation-job/";
    private static IAwsParameterStoreService _awsParameterStoreService;
    private static ProgramHelpers _programHelpers;

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
        DataMigrationAppSettings appSettings;

        if (vaultEnabled && vaultSource?.ToUpper() == "AWS")
        {
          appSettings = GetAWSConfiguration();
        }
        else
        {
          appSettings = GetConfigurationDetails(hostContext);
        }

        ConfigureServices(services, appSettings);
        ConfigureContexts(services, appSettings);
				ConfigureHttpClients(services, appSettings);
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
      vaultSource = configBuilder.GetValue<string>("Source");

      if (!vaultEnabled)
      {
        config.AddJsonFile("appsecrets.json", optional: false, reloadOnChange: true);
      }
    }

    private static void ConfigureContexts(IServiceCollection services, DataMigrationAppSettings appSettings)
    {
      services.AddScoped(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
      services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(appSettings.DbConnection));
    }

    private static DataMigrationAppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      string dbConnection;
      string conclaveLoginUrl;
      DataMigrationJobSettings fileUploadJob;
			WrapperApiSettings wrapperApiSettings;
      CiiAPI ciiAPI;
      Model.S3ConfigurationInfo s3configInfo;

			var config = hostContext.Configuration;
      dbConnection = config["DbConnection"];
      conclaveLoginUrl = config["ConclaveLoginUrl"];
      fileUploadJob = config.GetSection("DataMigrationJobSettings").Get<DataMigrationJobSettings>();
      wrapperApiSettings = config.GetSection("WrapperApiSettings").Get<WrapperApiSettings>();
      ciiAPI=config.GetSection("Cii").Get<CiiAPI>();
      s3configInfo = config.GetSection("S3ConfigurationInfo").Get<Model.S3ConfigurationInfo>();



      var appSettings = new DataMigrationAppSettings()
      {
        DbConnection = dbConnection,
        ConclaveLoginUrl=conclaveLoginUrl,
        DataMigrationJobSettings=new DataMigrationJobSettings
        {
          DataMigrationFileUploadJobFrequencyInMinutes=fileUploadJob.DataMigrationFileUploadJobFrequencyInMinutes
        },
        WrapperApiSettings = new WrapperApiSettings
        {
          OrgApiKey = wrapperApiSettings.OrgApiKey,
          ApiGatewayEnabledOrgUrl = wrapperApiSettings.ApiGatewayEnabledOrgUrl,
          ApiGatewayDisabledOrgUrl = wrapperApiSettings.ApiGatewayDisabledOrgUrl
        },
        CiiAPI=new CiiAPI
        {
          Url=ciiAPI.Url,
          Token=ciiAPI.Token
        },
        S3configInfo=new Model.S3ConfigurationInfo
        {
         AccessKeyId=s3configInfo.AccessKeyId,
          AccessSecretKey=s3configInfo.AccessSecretKey,
          ServiceUrl=s3configInfo.ServiceUrl,
          FileAccessExpirationInHours=s3configInfo.FileAccessExpirationInHours,
          DataMigrationBucketName=s3configInfo.DataMigrationBucketName,
          DataMigrationTemplateFolderName=s3configInfo.DataMigrationTemplateFolderName,
          DataMigrationFolderName=s3configInfo.DataMigrationFolderName,
          DataMigrationSuccessFolderName=s3configInfo.DataMigrationSuccessFolderName,
          DataMigrationFailedFolderName=s3configInfo.DataMigrationFailedFolderName
        }
			};
      return appSettings;
    }
		private static void ConfigureHttpClients(IServiceCollection services, DataMigrationAppSettings appSettings)
		{
			services.AddHttpClient("OrgWrapperApi", c =>
			{
				c.BaseAddress = new Uri(appSettings.IsApiGatewayEnabled ? appSettings.WrapperApiSettings.ApiGatewayEnabledOrgUrl : appSettings.WrapperApiSettings.ApiGatewayDisabledOrgUrl);
				c.DefaultRequestHeaders.Add("X-API-Key", appSettings.WrapperApiSettings.OrgApiKey);
			});
      services.AddHttpClient("CiiApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.CiiAPI.Url);
        c.DefaultRequestHeaders.Add("x-api-key", appSettings.CiiAPI.Token);
      });
    }
		private static void ConfigureServices(IServiceCollection services, DataMigrationAppSettings appSettings)
    {
      services.AddSingleton(s => appSettings);
      

      services.AddSingleton<ApplicationConfigurationInfo, ApplicationConfigurationInfo>();
      services.AddHttpClient();
      services.AddSingleton<IFileUploadJobService, FileUploadJobService>();

      services.AddScoped<IAuditLoginService, AuditLoginService>();
      services.AddScoped<IDateTimeService, DateTimeService>();
      services.AddScoped<IServiceRoleGroupMapperService, ServiceRoleGroupMapperService>();
      services.AddScoped<IWrapperApiService, WrapperApiService>();
      services.AddScoped<IWrapperOrganisationService, WrapperOrganisationService>();
      services.AddSingleton<IAwsS3Service, AwsS3Service>();
      services.AddSingleton(s =>
      {
          var s3Configuration = new S3ConfigurationInfo
          {
            ServiceUrl = appSettings.S3configInfo.ServiceUrl,
            AccessKeyId = appSettings.S3configInfo.AccessKeyId,
            AccessSecretKey = appSettings.S3configInfo.AccessSecretKey,
            FileAccessExpirationInHours = appSettings.S3configInfo.FileAccessExpirationInHours,
            DataMigrationBucketName = appSettings.S3configInfo.DataMigrationBucketName,
            DataMigrationTemplateFolderName = appSettings.S3configInfo.DataMigrationTemplateFolderName,
            DataMigrationSuccessFolderName = appSettings.S3configInfo.DataMigrationSuccessFolderName,
            DataMigrationFailedFolderName=appSettings.S3configInfo.DataMigrationFailedFolderName,
            DataMigrationFolderName=appSettings.S3configInfo.DataMigrationFolderName
          };

          return s3Configuration;
        });
    }

    private static DataMigrationAppSettings GetAWSConfiguration()
    {
      string dbConnection;
      DataMigrationJobSettings dataMigrationJobSettings;
			WrapperApiSettings wrapperApiSettings;
      CiiAPI ciiAPI;
      Model.S3ConfigurationInfo s3configInfo;
      
			_programHelpers = new ProgramHelpers();
      _awsParameterStoreService = new AwsParameterStoreService();

      var parameters = _programHelpers.LoadAwsSecretsAsync(_awsParameterStoreService).Result;

			var dbName = _awsParameterStoreService.FindParameterByName(parameters, path + "DbName");
      var dbConnectionEndPoint = _awsParameterStoreService.FindParameterByName(parameters, path + "DbConnection");
      var conclaveLoginUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "conclaveLoginUrl");
			

			if (!string.IsNullOrEmpty(dbName))
      {
        dbConnection = UtilityHelper.GetDatbaseConnectionString(dbName, dbConnectionEndPoint);
      }
      else
      {
        dbConnection = dbConnectionEndPoint;
      }

      ReadFromAWS(out dataMigrationJobSettings, parameters);
      ReadFromAWS(out wrapperApiSettings, parameters);
      ReadFromAWS(out ciiAPI, parameters);
      ReadFromAWS(out s3configInfo, parameters);

			return new DataMigrationAppSettings()
      {
        DbConnection = dbConnection,
        ConclaveLoginUrl= conclaveLoginUrl,
				WrapperApiSettings = wrapperApiSettings,
        CiiAPI= ciiAPI
      };
    }
    private static void ReadFromAWS(out DataMigrationJobSettings dataMigrationJobSettings, List<Parameter> parameters)
    {
      dataMigrationJobSettings = (DataMigrationJobSettings)_programHelpers.FillAwsParamsValue(typeof(DataMigrationJobSettings), parameters);
    }

		private static void ReadFromAWS(out WrapperApiSettings wrapperApiSettings, List<Parameter> parameters)
		{
			wrapperApiSettings = (WrapperApiSettings)_programHelpers.FillWrapperApiSettingsAwsParamsValue(typeof(WrapperApiSettings), parameters);
		}
    private static void ReadFromAWS(out CiiAPI ciiAPI, List<Parameter> parameters)
    {
      ciiAPI = (CiiAPI)_programHelpers.FillCiiApiAwsParamsValue(typeof(CiiAPI), parameters);
    }
    private static void ReadFromAWS(out Model.S3ConfigurationInfo s3configInfo, List<Parameter> parameters)
    {
      s3configInfo = (Model.S3ConfigurationInfo)_programHelpers.FillS3ConfigInfo(typeof(Model.S3ConfigurationInfo), parameters);
    }
    private static void ConfigureJobs(IServiceCollection services)
    {
      services.AddHostedService<FileUploadJob>();
    }
  }
}