using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.DelegationJobScheduler.Jobs;
using CcsSso.Core.DelegationJobScheduler.Model;
using CcsSso.Core.DelegationJobScheduler.Services;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Contracts.Wrapper;
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


namespace CcsSso.Core.DelegationJobScheduler
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
        DelegationAppSettings appSettings;

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

    private static void ConfigureContexts(IServiceCollection services, DelegationAppSettings appSettings)
    {
      services.AddScoped(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
      services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(appSettings.DbConnection));
    }

    private static DelegationAppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      string dbConnection;
      string conclaveLoginUrl;     
			DelegationJobSettings scheduleJob;
      DelegationExpiryNotificationJobSettings expiryNotificationJob;
      EmailSettings emailSettings;
			WrapperApiSettings wrapperApiSettings;
      NotificationApiSettings notificationApiSettings;

			var config = hostContext.Configuration;
			bool.TryParse(config["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);
			dbConnection = config["DbConnection"];
      conclaveLoginUrl = config["ConclaveLoginUrl"];
      scheduleJob = config.GetSection("DelegationJobSettings").Get<DelegationJobSettings>();
      expiryNotificationJob = config.GetSection("DelegationExpiryNotificationJobSettings").Get<DelegationExpiryNotificationJobSettings>();
      emailSettings = config.GetSection("EmailSettings").Get<EmailSettings>();
      wrapperApiSettings = config.GetSection("WrapperApiSettings").Get<WrapperApiSettings>();
      notificationApiSettings = config.GetSection("NotificationApiSettings").Get<NotificationApiSettings>();

      var appSettings = new DelegationAppSettings()
      {
        IsApiGatewayEnabled = isApiGatewayEnabled,
				DbConnection = dbConnection,
        ConclaveLoginUrl=conclaveLoginUrl,
        DelegationJobSettings = new DelegationJobSettings
        {
          DelegationLinkExpiryJobFrequencyInMinutes = scheduleJob.DelegationLinkExpiryJobFrequencyInMinutes,
          DelegationTerminationJobFrequencyInMinutes = scheduleJob.DelegationTerminationJobFrequencyInMinutes
        },
        DelegationExpiryNotificationJobSettings = new DelegationExpiryNotificationJobSettings
        {
          JobFrequencyInMinutes = expiryNotificationJob.JobFrequencyInMinutes,
          ExpiryNoticeInMinutes = expiryNotificationJob.ExpiryNoticeInMinutes,
         
        },
        EmailSettings=new EmailSettings
        {
          ApiKey=emailSettings.ApiKey,
          DelegationExpiryNotificationToAdminTemplateId=emailSettings.DelegationExpiryNotificationToAdminTemplateId,
          DelegationExpiryNotificationToUserTemplateId=emailSettings.DelegationExpiryNotificationToUserTemplateId,
        },
        WrapperApiSettings = new WrapperApiSettings
        {
          UserApiKey = wrapperApiSettings.UserApiKey,
          ApiGatewayEnabledUserUrl = wrapperApiSettings.ApiGatewayEnabledUserUrl,
          ApiGatewayDisabledUserUrl = wrapperApiSettings.ApiGatewayDisabledUserUrl
        },
				NotificationApiSettings = new NotificationApiSettings
				{
					 NotificationApiUrl = notificationApiSettings.NotificationApiUrl,
					NotificationApiKey = notificationApiSettings.NotificationApiKey
				}

			};

      return appSettings;
    }
		private static void ConfigureHttpClients(IServiceCollection services, DelegationAppSettings appSettings)
		{
			services.AddHttpClient("UserWrapperApi", c =>
			{
				c.BaseAddress = new Uri(appSettings.IsApiGatewayEnabled ? appSettings.WrapperApiSettings.ApiGatewayEnabledUserUrl : appSettings.WrapperApiSettings.ApiGatewayDisabledUserUrl);
				c.DefaultRequestHeaders.Add("X-API-Key", appSettings.WrapperApiSettings.UserApiKey);
			});
			services.AddHttpClient("NotificationApi", c =>
			{
				c.BaseAddress = new Uri(appSettings.NotificationApiSettings.NotificationApiUrl);
				c.DefaultRequestHeaders.Add("X-API-Key", appSettings.NotificationApiSettings.NotificationApiKey);
			});
		}
		private static void ConfigureServices(IServiceCollection services, DelegationAppSettings appSettings)
    {
      services.AddSingleton(s => appSettings);

      services.AddSingleton(s =>
      {
        EmailConfigurationInfo emailConfigurationInfo = new()
        {
          ApiKey = appSettings.EmailSettings.ApiKey,
        };

        return emailConfigurationInfo;
      });

      services.AddSingleton<ApplicationConfigurationInfo, ApplicationConfigurationInfo>();
     
      services.AddHttpClient();
      services.AddSingleton<IEmailProviderService, EmailProviderService>();


      services.AddScoped<IAuditLoginService, AuditLoginService>();
      services.AddScoped<IDateTimeService, DateTimeService>();
      services.AddScoped<IServiceRoleGroupMapperService, ServiceRoleGroupMapperService>();


      services.AddScoped<IUserProfileHelperService, UserProfileHelperService>();
      services.AddScoped<IExternalHelperService, ExternalHelperService>();
      services.AddScoped<IDelegationAuditEventService, DelegationAuditEventService>();
      services.AddScoped<IDelegationService, DelegationService>();
      services.AddScoped<IDelegationExpiryNotificationService, DelegationExpiryNotificationService>();
      services.AddScoped<IWrapperApiService, WrapperApiService>();
      services.AddScoped<IWrapperUserService, WrapperUserService>();
    }

    private static DelegationAppSettings GetAWSConfiguration()
    {
      string dbConnection;
      DelegationJobSettings delegationJobSettings;
      DelegationExpiryNotificationJobSettings delegationExpiryNotificationJobSettings;
      EmailSettings emailSettings;
			WrapperApiSettings wrapperApiSettings;
      NotificationApiSettings notificationApiSettings;
      
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

      ReadFromAWS(out delegationJobSettings, parameters);
      ReadFromAWS(out delegationExpiryNotificationJobSettings, parameters);
      ReadFromAWS(out emailSettings, parameters);
      ReadFromAWS(out wrapperApiSettings, parameters);
      ReadFromAWS(out notificationApiSettings, parameters);

			return new DelegationAppSettings()
      {
        DbConnection = dbConnection,
        ConclaveLoginUrl= conclaveLoginUrl,
        DelegationJobSettings = delegationJobSettings,
        DelegationExpiryNotificationJobSettings = delegationExpiryNotificationJobSettings,
        EmailSettings = emailSettings, 
				WrapperApiSettings = wrapperApiSettings,
				NotificationApiSettings = notificationApiSettings
			};
    }
		private static void ReadFromAWS(out DelegationJobSettings delegationJobSettings, List<Parameter> parameters)
    {
      delegationJobSettings = (DelegationJobSettings)_programHelpers.FillAwsParamsValue(typeof(DelegationJobSettings), parameters);
    }

    private static void ReadFromAWS(out DelegationExpiryNotificationJobSettings delegationExpiryNotificationJobSettings, List<Parameter> parameters)
    {
      delegationExpiryNotificationJobSettings = (DelegationExpiryNotificationJobSettings)_programHelpers.FillExpiryNotificationAwsParamsValue(typeof(DelegationExpiryNotificationJobSettings), parameters);
    }

    private static void ReadFromAWS(out EmailSettings emailSettings, List<Parameter> parameters)
    {
      emailSettings = (EmailSettings)_programHelpers.FillEmailSettingsAwsParamsValue(typeof(EmailSettings), parameters);
    }

		private static void ReadFromAWS(out WrapperApiSettings wrapperApiSettings, List<Parameter> parameters)
		{
			wrapperApiSettings = (WrapperApiSettings)_programHelpers.FillWrapperApiSettingsAwsParamsValue(typeof(WrapperApiSettings), parameters);
		}

		private static void ReadFromAWS(out NotificationApiSettings notificationApiSettings, List<Parameter> parameters)
		{
			notificationApiSettings = (NotificationApiSettings)_programHelpers.FillNotificationApiSettingsAwsParamsValue(typeof(NotificationApiSettings), parameters);
		}
		private static void ConfigureJobs(IServiceCollection services)
    {
      services.AddHostedService<LinkExpiryJob>();
      services.AddHostedService<DelegationTerminationJob>();
      services.AddHostedService<DelegationExpiryNotificationJob>();
    }
  }
}