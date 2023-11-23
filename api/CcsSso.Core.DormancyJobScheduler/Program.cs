using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Core.DormancyJobScheduler.Services;
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
using CcsSso.Core.DormancyJobScheduler.Contracts;
using CcsSso.Core.DormancyJobScheduler.Jobs;
using CcsSso.Core.DormancyJobScheduler.Services;
using CcsSso.Core.DormancyJobScheduler.Helper;

namespace CcsSso.Core.DormancyJobScheduler
{
  public class Program
  {
    private static bool vaultEnabled;
    private static string vaultSource;
    private static string path = "/conclave-sso/dormancy-job/";
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
        DormancyAppSettings appSettings;

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
        ConfigureJobs(services, appSettings);
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

    private static void ConfigureContexts(IServiceCollection services, DormancyAppSettings appSettings)
    {
      services.AddScoped(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
    }

    private static DormancyAppSettings GetConfigurationDetails(HostBuilderContext hostContext)
    {
      DormancyJobSettings scheduleJob;
      WrapperApiSettings wrapperApiSettings;
      SecurityApiSettings securityApiSettings;
      EmailSettings emailSettings;
      NotificationApiSettings notificationApiSettings;
      Auth0ConfigurationInfo auth0ConfigurationInfo;
      var config = hostContext.Configuration;
      bool.TryParse(config["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);
      scheduleJob = config.GetSection("DormancyJobSettings").Get<DormancyJobSettings>();
      wrapperApiSettings = config.GetSection("WrapperApiSettings").Get<WrapperApiSettings>();
      securityApiSettings = config.GetSection("SecurityApiSettings").Get<SecurityApiSettings>();
      emailSettings = config.GetSection("EmailSettings").Get<EmailSettings>();
      notificationApiSettings = config.GetSection("NotificationApiSettings").Get<NotificationApiSettings>();
      auth0ConfigurationInfo = config.GetSection("Auth0").Get<Auth0ConfigurationInfo>();

      var appSettings = new DormancyAppSettings()
      {
        IsApiGatewayEnabled = isApiGatewayEnabled,
        DormancyJobSettings = new DormancyJobSettings
        {
          DormancyNotificationJobFrequencyInMinutes = scheduleJob.DormancyNotificationJobFrequencyInMinutes,
          DeactivationNotificationInMinutes = scheduleJob.DeactivationNotificationInMinutes,
          UserDeactivationDurationInMinutes = scheduleJob.UserDeactivationDurationInMinutes,
          UserDeactivationJobFrequencyInMinutes = scheduleJob.UserDeactivationJobFrequencyInMinutes,
          DormancyNotificationJobEnable = scheduleJob.DormancyNotificationJobEnable,
          UserDeactivationJobEnable = scheduleJob.UserDeactivationJobEnable,

        },
        WrapperApiSettings = new WrapperApiSettings
        {
          UserApiKey = wrapperApiSettings.UserApiKey,
          ApiGatewayEnabledUserUrl = wrapperApiSettings.ApiGatewayEnabledUserUrl,
          ApiGatewayDisabledUserUrl = wrapperApiSettings.ApiGatewayDisabledUserUrl
        },
        SecurityApiSettings = new SecurityApiSettings
        {
          Url = securityApiSettings.Url,
          ApiKey = securityApiSettings.ApiKey
        },
        EmailSettings = new EmailSettings
        {
          ApiKey = emailSettings.ApiKey,
          UserDormantNotificationTemplateId = emailSettings.UserDormantNotificationTemplateId
        },
        NotificationApiSettings = new NotificationApiSettings
        {
          NotificationApiUrl = notificationApiSettings.NotificationApiUrl,
          NotificationApiKey = notificationApiSettings.NotificationApiKey
        },
        Auth0ConfigurationInfo = new Auth0ConfigurationInfo
        {
          ClientId = auth0ConfigurationInfo.ClientId,
          ClientSecret = auth0ConfigurationInfo.ClientSecret,
          ManagementApiBaseUrl = auth0ConfigurationInfo.ManagementApiBaseUrl,
          ManagementApiIdentifier = auth0ConfigurationInfo.ManagementApiIdentifier,
        }
      };

      return appSettings;
    }
    private static void ConfigureHttpClients(IServiceCollection services, DormancyAppSettings appSettings)
    {
      services.AddHttpClient("UserWrapperApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.IsApiGatewayEnabled ? appSettings.WrapperApiSettings.ApiGatewayEnabledUserUrl : appSettings.WrapperApiSettings.ApiGatewayDisabledUserUrl);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.WrapperApiSettings.UserApiKey);
      });
      services.AddHttpClient("SecurityWrapperApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.SecurityApiSettings.Url);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.SecurityApiSettings.ApiKey);
      });
      services.AddHttpClient("NotificationApi", c =>
      {
        c.BaseAddress = new Uri(appSettings.NotificationApiSettings.NotificationApiUrl);
        c.DefaultRequestHeaders.Add("X-API-Key", appSettings.NotificationApiSettings.NotificationApiKey);
      });
    }
    private static void ConfigureServices(IServiceCollection services, DormancyAppSettings appSettings)
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
      services.AddSingleton<Auth0TokenHelper>();
      services.AddHttpClient();
      services.AddScoped<IDateTimeService, DateTimeService>();
      services.AddScoped<IAuth0Service, Auth0Service>();
      services.AddScoped<IUserDeactivationService, UserDeactivationService>();
      services.AddScoped<IDormancyNotificationService, DormancyNotificationService>();
      services.AddScoped<IWrapperApiService, WrapperApiService>();
      services.AddScoped<IWrapperUserService, WrapperUserService>();
      services.AddSingleton<IEmailProviderService, EmailProviderService>();
    }

    private static DormancyAppSettings GetAWSConfiguration()
    {
      DormancyJobSettings dormancyJobSettings;
      WrapperApiSettings wrapperApiSettings;
      SecurityApiSettings securityApiSettings;
      EmailSettings emailSettings;
      NotificationApiSettings notificationApiSettings;
      Auth0ConfigurationInfo auth0ConfigurationInfo;

      _programHelpers = new ProgramHelpers();
      _awsParameterStoreService = new AwsParameterStoreService();

      var parameters = _programHelpers.LoadAwsSecretsAsync(_awsParameterStoreService).Result;

      ReadFromAWS(out dormancyJobSettings, parameters);
      ReadFromAWS(out wrapperApiSettings, parameters);
      ReadFromAWS(out securityApiSettings, parameters);
      ReadFromAWS(out emailSettings, parameters);
      ReadFromAWS(out notificationApiSettings, parameters);
      ReadFromAWS(out auth0ConfigurationInfo, parameters);

      return new DormancyAppSettings()
      {
        DormancyJobSettings = dormancyJobSettings,
        WrapperApiSettings = wrapperApiSettings,
        SecurityApiSettings = securityApiSettings,
        EmailSettings = emailSettings,
        NotificationApiSettings = notificationApiSettings,
        Auth0ConfigurationInfo = auth0ConfigurationInfo,
      };
    }
    private static void ReadFromAWS(out DormancyJobSettings dormancyJobSettings, List<Parameter> parameters)
    {
      dormancyJobSettings = (DormancyJobSettings)_programHelpers.FillAwsParamsValue(typeof(DormancyJobSettings), parameters);
    }

    private static void ReadFromAWS(out WrapperApiSettings wrapperApiSettings, List<Parameter> parameters)
    {
      wrapperApiSettings = (WrapperApiSettings)_programHelpers.FillWrapperApiSettingsAwsParamsValue(typeof(WrapperApiSettings), parameters);
    }
    private static void ReadFromAWS(out SecurityApiSettings securityApiSettings, List<Parameter> parameters)
    {
      securityApiSettings = (SecurityApiSettings)_programHelpers.FillSecuritySettingsAwsParamsValue(typeof(SecurityApiSettings), parameters);
    }
    private static void ReadFromAWS(out EmailSettings emailSettings, List<Parameter> parameters)
    {
      emailSettings = (EmailSettings)_programHelpers.FillEmailSettingsAwsParamsValue(typeof(EmailSettings), parameters);
    }
    private static void ReadFromAWS(out NotificationApiSettings notificationApiSettings, List<Parameter> parameters)
    {
      notificationApiSettings = (NotificationApiSettings)_programHelpers.FillNotificationApiSettingsAwsParamsValue(typeof(NotificationApiSettings), parameters);
    }
    private static void ReadFromAWS(out Auth0ConfigurationInfo notificationApiSettings, List<Parameter> parameters)
    {
      notificationApiSettings = (Auth0ConfigurationInfo)_programHelpers.FillAuth0SettingsAwsParamsValue(typeof(Auth0ConfigurationInfo), parameters);
    }
    private static void ConfigureJobs(IServiceCollection services, DormancyAppSettings appSettings)
    {
      if (appSettings.DormancyJobSettings.DormancyNotificationJobEnable)
        services.AddHostedService<DormancyNotificationJob>();

      if (appSettings.DormancyJobSettings.UserDeactivationJobEnable)
        services.AddHostedService<UserDeactivationJob>();
    }
  }
}
