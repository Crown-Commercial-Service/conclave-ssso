using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DelegationJobScheduler.Contracts;
using CcsSso.Core.DelegationJobScheduler.Jobs;
using CcsSso.Core.DelegationJobScheduler.Model;
using CcsSso.Core.DelegationJobScheduler.Services;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Service;
using CcsSso.Core.Service.External;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
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
      DelegationJobSettings scheduleJob;

      var config = hostContext.Configuration;
      dbConnection = config["DbConnection"];
      scheduleJob = config.GetSection("DelegationJobSettings").Get<DelegationJobSettings>();

      var appSettings = new DelegationAppSettings()
      {
        DbConnection = dbConnection,
        DelegationJobSettings = new DelegationJobSettings
        {
          DelegationLinkExpiryJobFrequencyInMinutes = scheduleJob.DelegationLinkExpiryJobFrequencyInMinutes,
          DelegationTerminationJobFrequencyInMinutes = scheduleJob.DelegationTerminationJobFrequencyInMinutes
        }
      };

      return appSettings;
    }

    private static void ConfigureServices(IServiceCollection services, DelegationAppSettings appSettings)
    {
      services.AddSingleton(s => appSettings);
      services.AddSingleton<ApplicationConfigurationInfo, ApplicationConfigurationInfo>();

      services.AddScoped<IAuditLoginService, AuditLoginService>();
      services.AddScoped<IDateTimeService, DateTimeService>();
      services.AddScoped<IServiceRoleGroupMapperService, ServiceRoleGroupMapperService>();

      services.AddScoped<IUserProfileHelperService, UserProfileHelperService>();
      services.AddScoped<IDelegationAuditEventService, DelegationAuditEventService>();
      services.AddScoped<IDelegationService, DelegationService>();
    }

    private static DelegationAppSettings GetAWSConfiguration()
    {
      string dbConnection;
      DelegationJobSettings delegationJobSettings;

      _programHelpers = new ProgramHelpers();
      _awsParameterStoreService = new AwsParameterStoreService();

      var parameters = _programHelpers.LoadAwsSecretsAsync(_awsParameterStoreService).Result;

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

      ReadFromAWS(out delegationJobSettings, parameters);

      return new DelegationAppSettings()
      {
        DbConnection = dbConnection,
        DelegationJobSettings = delegationJobSettings
      };
    }

    private static void ReadFromAWS(out DelegationJobSettings delegationJobSettings, List<Parameter> parameters)
    {
      delegationJobSettings = (DelegationJobSettings)_programHelpers.FillAwsParamsValue(typeof(DelegationJobSettings), parameters);
    }

    private static void ConfigureJobs(IServiceCollection services)
    {
      services.AddHostedService<LinkExpiryJob>();
      services.AddHostedService<DelegationTerminationJob>();
    }
  }
}