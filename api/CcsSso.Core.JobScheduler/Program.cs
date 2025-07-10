using Amazon.Runtime;
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Jobs;
using CcsSso.Core.JobScheduler.Services;
using CcsSso.Core.Service;
using CcsSso.Core.Service.Wrapper;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Domain.Helpers;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace CcsSso.Core.JobScheduler
{
	public class Program
	{
		private static bool vaultEnabled;
		private static string vaultSource;
		private static string path = "/conclave-sso/org-dereg-job/";
		private static IAwsParameterStoreService _awsParameterStoreService;
		private static ProgramHelpers _programHelpers;

		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
				Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((hostingContext, config) =>
				{
					String envName = "";

					if (!String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")))
					{
						envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
					}
					
					var configBuilder = new ConfigurationBuilder()
												 .AddJsonFile("appsettings.json", optional: false)
												 .AddJsonFile($"appsettings.{envName}.json", optional: true)
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
					CiiSettings ciiSettings;
					List<UserDeleteJobSetting> userDeleteJobSettings;
					SecurityApiSettings securityApiSettings;
					WrapperApiSettings wrapperApiSettings;
					ScheduleJobSettings scheduleJobSettings;
					BulkUploadSettings bulkUploadSettings;
					RedisCacheSettingsVault redisCacheSettingsVault;
					EmailConfigurationInfo emailConfigurationInfo;
					DocUploadInfoVault docUploadConfig;
					S3ConfigurationInfoVault s3ConfigurationInfo;
					OrgAutoValidationJobSettings orgAutoValidationJobSettings;
					OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles;
					OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob;
					OrgAutoValidationOneTimeJobEmail orgAutoValidationOneTimeJobEmail;
					ActiveJobStatus activeJobStatus;
					ServiceRoleGroupSettings serviceRoleGroupSettings;
					NotificationApiSettings notificationApiSettings;


          bool isApiGatewayEnabled = false;


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

							isApiGatewayEnabled = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "IsApiGatewayEnabled"));

							ReadFromAWS(out ciiSettings, out userDeleteJobSettings, out securityApiSettings, out wrapperApiSettings, out scheduleJobSettings, out bulkUploadSettings,
								out redisCacheSettingsVault, out emailConfigurationInfo, out docUploadConfig, out s3ConfigurationInfo, out orgAutoValidationJobSettings,
								 out orgAutoValidationOneTimeJob, out orgAutoValidationOneTimeJobRoles, out orgAutoValidationOneTimeJobEmail, out serviceRoleGroupSettings, out notificationApiSettings ,parameters);
							ReadFromAWSActiveJobStatus(out activeJobStatus, parameters);
						}
						else
						{
							ReadFromHashicorp(out dbConnection, out ciiSettings, out userDeleteJobSettings, out securityApiSettings, out wrapperApiSettings, out scheduleJobSettings,
								out bulkUploadSettings, out redisCacheSettingsVault, out emailConfigurationInfo, out docUploadConfig, out s3ConfigurationInfo, out orgAutoValidationJobSettings,
								out orgAutoValidationOneTimeJob, out orgAutoValidationOneTimeJobRoles, out orgAutoValidationOneTimeJobEmail, out isApiGatewayEnabled,
								out serviceRoleGroupSettings, out notificationApiSettings);
							ReadFromHashicorpActiveJobStatus(out activeJobStatus);
						}
					}
					else
					{
						ReadFromAppSecret(hostContext, out dbConnection, out ciiSettings, out userDeleteJobSettings, out securityApiSettings, out wrapperApiSettings, out scheduleJobSettings,
							out bulkUploadSettings, out redisCacheSettingsVault, out emailConfigurationInfo, out docUploadConfig, out s3ConfigurationInfo, out orgAutoValidationJobSettings,
							out orgAutoValidationOneTimeJob, out orgAutoValidationOneTimeJobRoles, out orgAutoValidationOneTimeJobEmail, out isApiGatewayEnabled,
							out serviceRoleGroupSettings, out notificationApiSettings);
						ReadFromAppSecretActiveJobStatus(hostContext, out activeJobStatus);
					}

					services.AddSingleton(s =>
					{
						return new AppSettings()
						{
							DbConnection = dbConnection,
							UserDeleteJobSettings = userDeleteJobSettings,
							WrapperApiSettings = new WrapperApiSettings()
							{
								ApiKey = wrapperApiSettings.ApiKey,
								Url = wrapperApiSettings.Url,
								ApiGatewayEnabledUserUrl = wrapperApiSettings.ApiGatewayEnabledUserUrl,
								ApiGatewayDisabledUserUrl = wrapperApiSettings.ApiGatewayDisabledUserUrl,
								ConfigApiKey = wrapperApiSettings.ConfigApiKey,
								OrgApiKey = wrapperApiSettings.OrgApiKey,
                OrgDeleteApiKey = wrapperApiSettings.OrgDeleteApiKey,
								ContactApiKey = wrapperApiSettings.ContactApiKey,
								RegistryContactDeleteApiKey = wrapperApiSettings.RegistryContactDeleteApiKey,
                ApiGatewayDisabledConfigUrl = wrapperApiSettings.ApiGatewayDisabledConfigUrl,
								ApiGatewayDisabledOrgUrl = wrapperApiSettings.ApiGatewayDisabledOrgUrl,
								ApiGatewayEnabledConfigUrl = wrapperApiSettings.ApiGatewayEnabledConfigUrl,
								ApiGatewayEnabledOrgUrl = wrapperApiSettings.ApiGatewayEnabledOrgUrl,
								ApiGatewayDisabledContactUrl = wrapperApiSettings.ApiGatewayDisabledContactUrl,
								ApiGatewayEnabledContactUrl = wrapperApiSettings.ApiGatewayEnabledContactUrl,
							},
							SecurityApiSettings = new SecurityApiSettings()
							{
								ApiKey = securityApiSettings.ApiKey,
								Url = securityApiSettings.Url
							},
							ScheduleJobSettings = scheduleJobSettings,
							BulkUploadSettings = bulkUploadSettings,
							CiiSettings = new CiiSettings()
							{
								Token = ciiSettings.Token,
								Url = ciiSettings.Url
							},
							OrgAutoValidationJobSettings = orgAutoValidationJobSettings,
							OrgAutoValidationOneTimeJob = orgAutoValidationOneTimeJob,
							OrgAutoValidationOneTimeJobRoles = orgAutoValidationOneTimeJobRoles,
							OrgAutoValidationOneTimeJobEmail = orgAutoValidationOneTimeJobEmail,
							IsApiGatewayEnabled = isApiGatewayEnabled,
							ActiveJobStatus = activeJobStatus,
							ServiceRoleGroupSettings = new ServiceRoleGroupSettings()
							{
								Enable = serviceRoleGroupSettings.Enable
							},
							NotificationApiSettings = new NotificationApiSettings()
							{
								NotificationApiUrl = notificationApiSettings.NotificationApiUrl,
								NotificationApiKey = notificationApiSettings.NotificationApiKey
							}
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
					services.AddSingleton(s =>
					{
						return emailConfigurationInfo;
					});

					services.AddSingleton(s =>
					{
						int.TryParse(docUploadConfig.SizeValidationValue, out int sizeValidationValue);
						sizeValidationValue = sizeValidationValue == 0 ? 100000000 : sizeValidationValue;

						return new DocUploadConfig
						{
							BaseUrl = docUploadConfig.Url,
							Token = docUploadConfig.Token,
							DefaultTypeValidationValue = docUploadConfig.TypeValidationValue,
							DefaultSizeValidationValue = sizeValidationValue
						};
					});

					services.AddSingleton(s =>
					{

						int.TryParse(s3ConfigurationInfo.FileAccessExpirationInHours, out int fileAccessExpirationInHours);
						fileAccessExpirationInHours = fileAccessExpirationInHours == 0 ? 36 : fileAccessExpirationInHours;

						return new S3ConfigurationInfo
						{
							AccessKeyId = s3ConfigurationInfo.AccessKeyId,
							AccessSecretKey = s3ConfigurationInfo.AccessSecretKey,
							BulkUploadBucketName = s3ConfigurationInfo.BulkUploadBucketName,
							BulkUploadFolderName = s3ConfigurationInfo.BulkUploadFolderName,
							BulkUploadTemplateFolderName = s3ConfigurationInfo.BulkUploadTemplateFolderName,
							ServiceUrl = s3ConfigurationInfo.ServiceUrl,
							FileAccessExpirationInHours = fileAccessExpirationInHours
						};
					});

					services.AddSingleton<IDateTimeService, DateTimeService>();
					services.AddSingleton<IIdamSupportService, IdamSupportService>();
					services.AddSingleton<IEmailSupportService, EmailSupportService>();
					services.AddSingleton<IEmailProviderService, EmailProviderService>();
					services.AddSingleton<RequestContext>(s => new RequestContext { UserId = -1 }); // Set context user id to -1 to identify the updates done by the job
					services.AddSingleton<IRemoteCacheService, RedisCacheService>();
               services.AddSingleton<ICacheInvalidateService, CacheInvalidateService>();
               services.AddSingleton<RedisConnectionPoolService>(_ =>
						new RedisConnectionPoolService(redisCacheSettingsVault.ConnectionString)
					);
					services.AddSingleton<IAwsS3Service, AwsS3Service>();
					services.AddSingleton<ApplicationConfigurationInfo, ApplicationConfigurationInfo>();

					
					services.AddScoped<IRoleApprovalLinkExpiredService, RoleApprovalLinkExpiredService>();
					// #Auto validation					
					
					services.AddSingleton<IAutoValidationService, AutoValidationService>();					
					services.AddSingleton<IWrapperApiService, WrapperApiService>();
					services.AddSingleton<IWrapperUserService, WrapperUserService>();
					services.AddSingleton<IWrapperConfigurationService, WrapperConfigurationService>();
					services.AddSingleton<IWrapperOrganisationService, WrapperOrganisationService>();
          services.AddSingleton<IWrapperContactService, WrapperContactService>();

					services.AddHostedService<OrganisationDeleteForInactiveRegistrationJob>();
					services.AddHostedService<UnverifiedUserDeleteJob>();
					//MS: As per the discussion with Ruban, this 2 jobs not required.
					//services.AddHostedService<BulkUploadMigrationStatusCheckJob>();
					//services.AddHostedService<OrganisationAutovalidationJob>();
					services.AddHostedService<RoleApprovalLinkExpiredJob>();

					services.AddSingleton<IWrapperUserService, WrapperUserService>();
					services.AddSingleton<IWrapperOrganisationService, WrapperOrganisationService>();
					services.AddSingleton<IWrapperSecurityService, WrapperSecurityService>();
					ConfigureHttpClients(services, ciiSettings, securityApiSettings, wrapperApiSettings, isApiGatewayEnabled, notificationApiSettings);
				});

		private static void ConfigureHttpClients(IServiceCollection services, CiiSettings _ciiSettings, SecurityApiSettings _securityApiSettings, WrapperApiSettings _wrapperApiSettings, bool isApiGatewayEnabled, NotificationApiSettings _notificationApiSettings)
		{
			services.AddHttpClient("UserWrapperApi", c =>
			{
				c.BaseAddress = new Uri(isApiGatewayEnabled ? _wrapperApiSettings.ApiGatewayEnabledUserUrl : _wrapperApiSettings.ApiGatewayDisabledUserUrl);
				c.DefaultRequestHeaders.Add("x-api-key", _wrapperApiSettings.UserApiKey);
			});

			services.AddHttpClient("ConfigWrapperApi", c =>
			{
				c.BaseAddress = new Uri(isApiGatewayEnabled ? _wrapperApiSettings.ApiGatewayEnabledConfigUrl : _wrapperApiSettings.ApiGatewayDisabledConfigUrl);
				c.DefaultRequestHeaders.Add("x-api-key", _wrapperApiSettings.ConfigApiKey);
			});

			services.AddHttpClient("OrgWrapperApi", c =>
			{
				c.BaseAddress = new Uri(isApiGatewayEnabled ? _wrapperApiSettings.ApiGatewayEnabledOrgUrl : _wrapperApiSettings.ApiGatewayDisabledOrgUrl);
				c.DefaultRequestHeaders.Add("x-api-key", _wrapperApiSettings.OrgApiKey);
			});

      services.AddHttpClient("OrgWrapperDeleteApi", c =>
      {
          c.BaseAddress = new Uri(isApiGatewayEnabled ? _wrapperApiSettings.ApiGatewayEnabledOrgUrl : _wrapperApiSettings.ApiGatewayDisabledOrgUrl);
          c.DefaultRequestHeaders.Add("x-api-key", _wrapperApiSettings.OrgDeleteApiKey);
      });
      services.AddHttpClient("ContactWrapperDeleteApi", c =>
      {
        c.BaseAddress = new Uri(isApiGatewayEnabled ? _wrapperApiSettings.ApiGatewayEnabledContactUrl : _wrapperApiSettings.ApiGatewayDisabledContactUrl);
        c.DefaultRequestHeaders.Add("x-api-key", _wrapperApiSettings.RegistryContactDeleteApiKey);
      });
      // Question about security wrapper api settings where to place these settings.
      services.AddHttpClient("SecurityWrapperApi", c =>
			{
				c.BaseAddress = new Uri(_securityApiSettings.Url);
				c.DefaultRequestHeaders.Add("x-api-key", _securityApiSettings.ApiKey);
			});

			services.AddHttpClient("CiiWrapperApi", c =>
			{
				c.DefaultRequestHeaders.Add("x-api-key", _ciiSettings.Token);
				c.BaseAddress = new Uri(_ciiSettings.Url);
			});

			services.AddHttpClient("NotificationApi", c =>
			{
				c.BaseAddress = new Uri(_notificationApiSettings.NotificationApiUrl);
				c.DefaultRequestHeaders.Add("X-API-Key", _notificationApiSettings.NotificationApiKey);
			});

      services.AddHttpClient("ContactWrapperApi", c =>
      {
        c.BaseAddress = new Uri(isApiGatewayEnabled ? _wrapperApiSettings.ApiGatewayEnabledContactUrl : _wrapperApiSettings.ApiGatewayDisabledContactUrl);
        c.DefaultRequestHeaders.Add("x-api-key", _wrapperApiSettings.ContactApiKey);
      });
    }
		private static void ReadFromAppSecret(HostBuilderContext hostContext, out string dbConnection, out CiiSettings ciiSettings,
			out List<UserDeleteJobSetting> userDeleteJobSettings, out SecurityApiSettings securityApiSettings, out WrapperApiSettings wrapperApiSettings,
			out ScheduleJobSettings scheduleJobSettings, out BulkUploadSettings bulkUploadSettings, out RedisCacheSettingsVault redisCacheSettingsVault,
			out EmailConfigurationInfo emailConfigurationInfo, out DocUploadInfoVault docUploadConfig, out S3ConfigurationInfoVault s3ConfigurationInfo,
			out OrgAutoValidationJobSettings orgAutoValidationJobSettings,
			out OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob, out OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles,
			out OrgAutoValidationOneTimeJobEmail orgAutoValidationOneTimeJobEmail, out bool isApiGatewayEnabled,
			out ServiceRoleGroupSettings serviceRoleGroupSettings, out NotificationApiSettings notificationApiSettings)
		{
			var config = hostContext.Configuration;
			dbConnection = config["DbConnection"];
			ciiSettings = config.GetSection("CIISettings").Get<CiiSettings>();
			userDeleteJobSettings = config.GetSection("UserDeleteJobSettings").Get<List<UserDeleteJobSetting>>();
			wrapperApiSettings = config.GetSection("WrapperApiSettings").Get<WrapperApiSettings>();
			securityApiSettings = config.GetSection("SecurityApiSettings").Get<SecurityApiSettings>();
			scheduleJobSettings = config.GetSection("ScheduleJobSettings").Get<ScheduleJobSettings>();
			bulkUploadSettings = config.GetSection("BulkUploadSettings").Get<BulkUploadSettings>();
			emailConfigurationInfo = config.GetSection("Email").Get<EmailConfigurationInfo>();
			redisCacheSettingsVault = config.GetSection("RedisCacheSettings").Get<RedisCacheSettingsVault>();
			docUploadConfig = config.GetSection("DocUpload").Get<DocUploadInfoVault>();
			s3ConfigurationInfo = config.GetSection("S3ConfigurationInfo").Get<S3ConfigurationInfoVault>();
			// #Auto validation
			orgAutoValidationJobSettings = config.GetSection("OrgAutoValidation").Get<OrgAutoValidationJobSettings>();
			orgAutoValidationOneTimeJob = config.GetSection("OrgAutoValidationOneTimeJob").Get<OrgAutoValidationOneTimeJob>();
			orgAutoValidationOneTimeJobRoles = config.GetSection("OrgAutoValidationOneTimeJobRoles").Get<OrgAutoValidationOneTimeJobRoles>();
			orgAutoValidationOneTimeJobEmail = config.GetSection("OrgAutoValidationOneTimeJobEmail").Get<OrgAutoValidationOneTimeJobEmail>();
			serviceRoleGroupSettings = config.GetSection("ServiceRoleGroupSettings").Get<ServiceRoleGroupSettings>();
			isApiGatewayEnabled = Convert.ToBoolean(config["IsApiGatewayEnabled"]);
			notificationApiSettings = config.GetSection("NotificationApiSettings").Get<NotificationApiSettings>();
    }
		private static void ReadFromAppSecretActiveJobStatus(HostBuilderContext hostContext, out ActiveJobStatus activeJobStatus)
		{
			var config = hostContext.Configuration;
			activeJobStatus = config.GetSection("ActiveJobStatus").Get<ActiveJobStatus>();
		}

		private static void ReadFromHashicorp(out string dbConnection, out CiiSettings ciiSettings, out List<UserDeleteJobSetting> userDeleteJobSettings,
			out SecurityApiSettings securityApiSettings, out WrapperApiSettings wrapperApiSettings, out ScheduleJobSettings scheduleJobSettings,
			out BulkUploadSettings bulkUploadSettings, out RedisCacheSettingsVault redisCacheSettingsVault, out EmailConfigurationInfo emailConfigurationInfo,
			out DocUploadInfoVault docUploadConfig, out S3ConfigurationInfoVault s3ConfigurationInfo, out OrgAutoValidationJobSettings orgAutoValidationJobSettings,
			out OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob, out OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles,
			out OrgAutoValidationOneTimeJobEmail orgAutoValidationOneTimeJobEmail, out bool isApiGatewayEnabled,
			out ServiceRoleGroupSettings serviceRoleGroupSettings, out NotificationApiSettings notificationApiSettings)
		{
			var secrets = _programHelpers.LoadSecretsAsync().Result;
			dbConnection = secrets["DbConnection"].ToString();
			ciiSettings = JsonConvert.DeserializeObject<CiiSettings>(secrets["CIISettings"].ToString());
			userDeleteJobSettings = JsonConvert.DeserializeObject<List<UserDeleteJobSetting>>(secrets["UserDeleteJobSettings"].ToString());
			emailConfigurationInfo = JsonConvert.DeserializeObject<EmailConfigurationInfo>(secrets["Email"].ToString());
			wrapperApiSettings = JsonConvert.DeserializeObject<WrapperApiSettings>(secrets["WrapperApiSettings"].ToString());
			securityApiSettings = JsonConvert.DeserializeObject<SecurityApiSettings>(secrets["SecurityApiSettings"].ToString());
			scheduleJobSettings = JsonConvert.DeserializeObject<ScheduleJobSettings>(secrets["ScheduleJobSettings"].ToString());
			bulkUploadSettings = JsonConvert.DeserializeObject<BulkUploadSettings>(secrets["BulkUploadSettings"].ToString());
			redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(secrets["RedisCacheSettings"].ToString());
			docUploadConfig = JsonConvert.DeserializeObject<DocUploadInfoVault>(secrets["DocUpload"].ToString());
			s3ConfigurationInfo = JsonConvert.DeserializeObject<S3ConfigurationInfoVault>(secrets["S3ConfigurationInfo"].ToString());
			// #Auto validation
			orgAutoValidationJobSettings = JsonConvert.DeserializeObject<OrgAutoValidationJobSettings>(secrets["OrgAutoValidation"].ToString());
			orgAutoValidationOneTimeJob = JsonConvert.DeserializeObject<OrgAutoValidationOneTimeJob>(secrets["OrgAutoValidationOneTimeJob"].ToString());
			orgAutoValidationOneTimeJobRoles = JsonConvert.DeserializeObject<OrgAutoValidationOneTimeJobRoles>(secrets["OrgAutoValidationOneTimeJobRoles"].ToString());
			orgAutoValidationOneTimeJobEmail = JsonConvert.DeserializeObject<OrgAutoValidationOneTimeJobEmail>(secrets["OrgAutoValidationOneTimeJobEmail"].ToString());
			isApiGatewayEnabled = Convert.ToBoolean(secrets["IsApiGatewayEnabled"].ToString());
			serviceRoleGroupSettings = JsonConvert.DeserializeObject<ServiceRoleGroupSettings>(secrets["ServiceRoleGroupSettings"].ToString());
			notificationApiSettings = JsonConvert.DeserializeObject<NotificationApiSettings>(secrets["NotificationApiSettings"].ToString());
    }
		private static void ReadFromHashicorpActiveJobStatus(out ActiveJobStatus activeJobStatus)
		{
			var secrets = _programHelpers.LoadSecretsAsync().Result;
			activeJobStatus = JsonConvert.DeserializeObject<ActiveJobStatus>(secrets["ActiveJobStatus"].ToString());
		}


		private static void ReadFromAWS(out CiiSettings ciiSettings, out List<UserDeleteJobSetting> userDeleteJobSettings, out SecurityApiSettings securityApiSettings,
			out WrapperApiSettings wrapperApiSettings, out ScheduleJobSettings scheduleJobSettings, out BulkUploadSettings bulkUploadSettings,
			out RedisCacheSettingsVault redisCacheSettingsVault, out EmailConfigurationInfo emailConfigurationInfo, out DocUploadInfoVault docUploadConfig,
			out S3ConfigurationInfoVault s3ConfigurationInfo, out OrgAutoValidationJobSettings orgAutoValidationJobSettings,
			out OrgAutoValidationOneTimeJob orgAutoValidationOneTimeJob, out OrgAutoValidationOneTimeJobRoles orgAutoValidationOneTimeJobRoles,
			out OrgAutoValidationOneTimeJobEmail orgAutoValidationOneTimeJobEmail, out ServiceRoleGroupSettings serviceRoleGroupSettings, out NotificationApiSettings notificationApiSettings , List<Parameter> parameters)
		{
			ciiSettings = (CiiSettings)_programHelpers.FillAwsParamsValue(typeof(CiiSettings), parameters);
			userDeleteJobSettings = (List<UserDeleteJobSetting>)_programHelpers.FillAwsParamsValue(typeof(List<UserDeleteJobSetting>), parameters);
			emailConfigurationInfo = (EmailConfigurationInfo)_programHelpers.FillAwsParamsValue(typeof(EmailConfigurationInfo), parameters);
			wrapperApiSettings = (WrapperApiSettings)_programHelpers.FillAwsParamsValue(typeof(WrapperApiSettings), parameters);
			securityApiSettings = (SecurityApiSettings)_programHelpers.FillAwsParamsValue(typeof(SecurityApiSettings), parameters);
			scheduleJobSettings = (ScheduleJobSettings)_programHelpers.FillAwsParamsValue(typeof(ScheduleJobSettings), parameters);
			bulkUploadSettings = (BulkUploadSettings)_programHelpers.FillAwsParamsValue(typeof(BulkUploadSettings), parameters);
			redisCacheSettingsVault = (RedisCacheSettingsVault)_programHelpers.FillAwsParamsValue(typeof(RedisCacheSettingsVault), parameters);
			docUploadConfig = (DocUploadInfoVault)_programHelpers.FillAwsParamsValue(typeof(DocUploadInfoVault), parameters);
			s3ConfigurationInfo = (S3ConfigurationInfoVault)_programHelpers.FillAwsParamsValue(typeof(S3ConfigurationInfoVault), parameters);
			// #Auto validation
			orgAutoValidationJobSettings = (OrgAutoValidationJobSettings)_programHelpers.FillAwsParamsValue(typeof(OrgAutoValidationJobSettings), parameters);
			orgAutoValidationOneTimeJob = (OrgAutoValidationOneTimeJob)_programHelpers.FillAwsParamsValue(typeof(OrgAutoValidationOneTimeJob), parameters);
			orgAutoValidationOneTimeJobRoles = (OrgAutoValidationOneTimeJobRoles)_programHelpers.FillAwsParamsValue(typeof(OrgAutoValidationOneTimeJobRoles), parameters);
			orgAutoValidationOneTimeJobEmail = (OrgAutoValidationOneTimeJobEmail)_programHelpers.FillAwsParamsValue(typeof(OrgAutoValidationOneTimeJobEmail), parameters);
			serviceRoleGroupSettings = (ServiceRoleGroupSettings)_programHelpers.FillAwsParamsValue(typeof(ServiceRoleGroupSettings), parameters);
			notificationApiSettings =(NotificationApiSettings)_programHelpers.FillAwsParamsValue(typeof(NotificationApiSettings), parameters);
    }

		private static void ReadFromAWSActiveJobStatus(out ActiveJobStatus activeJobStatus, List<Parameter> parameters)
		{
			activeJobStatus = (ActiveJobStatus)_programHelpers.FillAwsParamsValue(typeof(ActiveJobStatus), parameters);
		}
	}
}
