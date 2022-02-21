using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Services;
using CcsSso.Core.Service;
using CcsSso.Core.Service.External;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Core.JobScheduler
{
  public class Program
  {
    private static bool vaultEnabled;
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
          ScheduleJobSettings scheduleJobSettings;
          BulkUploadSettings bulkUploadSettings;
          RedisCacheSettingsVault redisCacheSettingsVault;
          EmailConfigurationInfo emailConfigurationInfo;
          DocUploadInfoVault docUploadConfig;
          S3ConfigurationInfoVault s3ConfigurationInfo;

          if (vaultEnabled)
          {
            var secrets = LoadSecretsAsync().Result;
            dbConnection = secrets["DbConnection"].ToString();
            ciiSettings = JsonConvert.DeserializeObject<CiiSettings>(secrets["CIISettings"].ToString());
            userDeleteJobSettings = JsonConvert.DeserializeObject<List<UserDeleteJobSetting>>(secrets["UserDeleteJobSettings"].ToString());
            emailConfigurationInfo = JsonConvert.DeserializeObject<EmailConfigurationInfo>(secrets["Email"].ToString());
            securityApiSettings = JsonConvert.DeserializeObject<SecurityApiSettings>(secrets["SecurityApiSettings"].ToString());
            scheduleJobSettings = JsonConvert.DeserializeObject<ScheduleJobSettings>(secrets["ScheduleJobSettings"].ToString());
            bulkUploadSettings = JsonConvert.DeserializeObject<BulkUploadSettings>(secrets["BulkUploadSettings"].ToString());
            redisCacheSettingsVault = JsonConvert.DeserializeObject<RedisCacheSettingsVault>(secrets["RedisCacheSettings"].ToString());
            docUploadConfig = JsonConvert.DeserializeObject<DocUploadInfoVault>(secrets["DocUpload"].ToString());
            s3ConfigurationInfo = JsonConvert.DeserializeObject<S3ConfigurationInfoVault>(secrets["S3ConfigurationInfo"].ToString());
          }
          else
          {
            var config = hostContext.Configuration;
            dbConnection = config["DbConnection"];
            ciiSettings = config.GetSection("CIISettings").Get<CiiSettings>();
            userDeleteJobSettings = config.GetSection("UserDeleteJobSettings").Get<List<UserDeleteJobSetting>>();
            securityApiSettings = config.GetSection("SecurityApiSettings").Get<SecurityApiSettings>();
            scheduleJobSettings = config.GetSection("ScheduleJobSettings").Get<ScheduleJobSettings>();
            bulkUploadSettings = config.GetSection("BulkUploadSettings").Get<BulkUploadSettings>();
            emailConfigurationInfo = config.GetSection("Email").Get<EmailConfigurationInfo>();
            redisCacheSettingsVault = config.GetSection("RedisCacheSettings").Get<RedisCacheSettingsVault>();
            docUploadConfig = config.GetSection("DocUpload").Get<DocUploadInfoVault>();
            s3ConfigurationInfo = config.GetSection("S3ConfigurationInfo").Get<S3ConfigurationInfoVault>();
          }

          services.AddSingleton(s =>
          {
            return new AppSettings()
            {
              DbConnection = dbConnection,
              UserDeleteJobSettings = userDeleteJobSettings,
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

          services.AddSingleton(s => {

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

          services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(dbConnection));
          services.AddScoped<IOrganisationSupportService, OrganisationSupportService>();
          services.AddScoped<IContactSupportService, ContactSupportService>();
          services.AddScoped<IBulkUploadFileContentService, BulkUploadFileContentService>();
          services.AddScoped<IUserProfileHelperService, UserProfileHelperService>();

          services.AddHostedService<OrganisationDeleteForInactiveRegistrationJob>();
          services.AddHostedService<UnverifiedUserDeleteJob>();
          services.AddHostedService<BulkUploadMigrationStatusCheckJob>();

        });

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
      var _secrets = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/org-dereg-job", mountPathValue);
      return _secrets.Data;
    }
  }
}
