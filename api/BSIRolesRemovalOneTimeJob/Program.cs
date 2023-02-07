using Amazon.SimpleSystemsManagement.Model;
using BSIRolesRemovalOneTimeJob.Jobs;
using CcsSso.Core.BSIRolesRemovalOneTimeJob.Contracts;
using CcsSso.Core.BSIRolesRemovalOneTimeJob.Service;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.Service.External;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CcsSso.Core.BSIRolesRemovalOneTimeJob.Jobs
{
  public class Program
  {
    private static bool vaultEnabled;
    private static string vaultSource;
    private static string path = "/conclave-sso/bsi-role-removal-job/";
    private static IAwsParameterStoreService? _awsParameterStoreService;
    private static ProgramHelpers _programHelpers;

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
          vaultSource = configBuilder.GetValue<string>("Source");
          if (!vaultEnabled)
          {
            config.AddJsonFile("appsecrets.json", optional: false, reloadOnChange: true);
          }
        }).ConfigureServices((hostContext, services) =>
        {
          string dbConnection;
          WrapperApiSettings wrapperApiSettings;
          ScheduleJobSettings scheduleJobSettings;
          OrgAutoValidationJobSettings orgAutoValidationJobSettings;
          OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles;
          OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob;
          OrgAutoValidationOneTimeJobEmail orgAutoValidationOneTimeJobEmail;


          if (vaultEnabled)
          {
            _programHelpers = new ProgramHelpers();

            if (vaultSource?.ToUpper() == "AWS")
            {
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

              ReadFromAWS(out wrapperApiSettings,
                              out scheduleJobSettings,
                              out orgAutoValidationOneTimeJob,
                              out orgAutoValidationOneTimeJobRoles
                              , parameters);
            }
            else
            {
              ReadFromHashicorp(out dbConnection,
                                out wrapperApiSettings,
                                out scheduleJobSettings,                               
                                out orgAutoValidationOneTimeJob,
                                out orgAutoValidationOneTimeJobRoles);

            }
          }
          else
          {
            ReadFromAppSecret(hostContext, out dbConnection,
                              out wrapperApiSettings,
                              out scheduleJobSettings,                             
                              out orgAutoValidationOneTimeJob,
                              out orgAutoValidationOneTimeJobRoles);
          }

          services.AddSingleton(s =>
          {
            return new AppSettings()
            {
              DbConnection = dbConnection,
              WrapperApiSettings = new WrapperApiSettings()
              {
                ApiKey = wrapperApiSettings.ApiKey,
                Url = wrapperApiSettings.Url
              },
              ScheduleJobSettings = scheduleJobSettings,
              OrgAutoValidationOneTimeJob = orgAutoValidationOneTimeJob,
              OrgAutoValidationOneTimeJobRoles = orgAutoValidationOneTimeJobRoles
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


          services.AddSingleton<IDateTimeService, DateTimeService>();


          services.AddSingleton<RequestContext>(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
          services.AddSingleton<ApplicationConfigurationInfo, ApplicationConfigurationInfo>();

          services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(dbConnection));

          // #Auto validation
          //services.AddScoped<IOrganisationAuditService, OrganisationAuditService>();
          //services.AddScoped<IOrganisationAuditEventService, OrganisationAuditEventService>();

          services.AddSingleton<IRemoveRoleFromAllOrganisationService, RemoveRoleFromAllOrganisationService>();

          services.AddHostedService<RolesRemovalOneTimeJob>();
        });

    private static void ReadFromAppSecret(HostBuilderContext hostContext, out string dbConnection,
      out WrapperApiSettings wrapperApiSettings,
      out ScheduleJobSettings scheduleJobSettings,
      out OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob,
      out OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles)
    {
      var config = hostContext.Configuration;
      dbConnection = config["DbConnection"];
      wrapperApiSettings = config.GetSection("WrapperApiSettings").Get<WrapperApiSettings>();
      scheduleJobSettings = config.GetSection("ScheduleJobSettings").Get<ScheduleJobSettings>();
      orgAutoValidationOneTimeJob = config.GetSection("OrgAutoValidationOneTimeJob").Get<OrgAutoValidationOneTimeJob>();
      orgAutoValidationOneTimeJobRoles = config.GetSection("OrgAutoValidationOneTimeJobRoles").Get<OrgAutoValidationOneTimeJobRoles>();

    }

    private static void ReadFromHashicorp(
      out string dbConnection,
      out WrapperApiSettings wrapperApiSettings,
      out ScheduleJobSettings scheduleJobSettings,   
      out OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob,
      out OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles)
    {
      var secrets = _programHelpers.LoadSecretsAsync().Result;
      dbConnection = secrets["DbConnection"].ToString();

      wrapperApiSettings = JsonConvert.DeserializeObject<WrapperApiSettings>(secrets["WrapperApiSettings"].ToString());
      scheduleJobSettings = JsonConvert.DeserializeObject<ScheduleJobSettings>(secrets["ScheduleJobSettings"].ToString());
      // #Auto validation
     
      orgAutoValidationOneTimeJob = JsonConvert.DeserializeObject<OrgAutoValidationOneTimeJob>(secrets["OrgAutoValidationOneTimeJob"].ToString());
      orgAutoValidationOneTimeJobRoles = JsonConvert.DeserializeObject<OrgAutoValidationOneTimeJobRoles>(secrets["OrgAutoValidationOneTimeJobRoles"].ToString());
    }

    private static void ReadFromAWS(
      out WrapperApiSettings wrapperApiSettings,
      out ScheduleJobSettings scheduleJobSettings,
      out OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob,
      out OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles,
      List<Parameter> parameters)
    {
      scheduleJobSettings = (ScheduleJobSettings)_programHelpers.FillAwsParamsValue(typeof(ScheduleJobSettings), parameters);
      wrapperApiSettings = (WrapperApiSettings)_programHelpers.FillAwsParamsValue(typeof(WrapperApiSettings), parameters);
      // #Auto validation
      orgAutoValidationOneTimeJob = (OrgAutoValidationOneTimeJob)_programHelpers.FillAwsParamsValue(typeof(OrgAutoValidationOneTimeJob), parameters);
      orgAutoValidationOneTimeJobRoles = (OrgAutoValidationOneTimeJobRoles)_programHelpers.FillAwsParamsValue(typeof(OrgAutoValidationOneTimeJobRoles), parameters);
    }

  }
}
