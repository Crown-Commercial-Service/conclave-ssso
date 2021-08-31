using CcsSso.Adaptor.Domain.SqsListener;
using CcsSso.Adaptor.SqsListener.Listners;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Adaptor.SqsListener
{
  public class Program
  {
    private static bool vaultEnabled;

    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
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
              AdaptorApiSetting adaptorApiSettings;
              SqsListnerJobSettingVault sqsJobSettingsVault;
              QueueInfoVault queueInfoVault;
              string _isApiGatewayEnabled;

              if (vaultEnabled)
              {
                var secrets = LoadSecretsAsync().Result;
                _isApiGatewayEnabled = secrets["IsApiGatewayEnabled"].ToString();
                adaptorApiSettings = JsonConvert.DeserializeObject<AdaptorApiSetting>(secrets["AdaptorApiSettings"].ToString());
                sqsJobSettingsVault = JsonConvert.DeserializeObject<SqsListnerJobSettingVault>(secrets["SqsListnerJobSettings"].ToString());
                queueInfoVault = JsonConvert.DeserializeObject<QueueInfoVault>(secrets["QueueInfo"].ToString());
              }
              else
              {
                var config = hostContext.Configuration;
                _isApiGatewayEnabled = config["IsApiGatewayEnabled"];
                adaptorApiSettings = config.GetSection("AdaptorApiSettings").Get<AdaptorApiSetting>();
                sqsJobSettingsVault = config.GetSection("SqsListnerJobSettings").Get<SqsListnerJobSettingVault>();
                queueInfoVault = config.GetSection("QueueInfo").Get<QueueInfoVault>();
              }

              services.AddSingleton( s => {
                int.TryParse(sqsJobSettingsVault.JobSchedulerExecutionFrequencyInMinutes, out int jobSchedulerExecutionFrequencyInMinutes);
                int.TryParse(sqsJobSettingsVault.MessageReadThreshold, out int messageReadThreshold);

                if (jobSchedulerExecutionFrequencyInMinutes == 0)
                {
                  jobSchedulerExecutionFrequencyInMinutes = 10;
                }
                return new SqsListnerAppSetting
                {
                  SqsListnerJobSetting = new SqsListnerJobSetting
                  {
                    JobSchedulerExecutionFrequencyInMinutes = jobSchedulerExecutionFrequencyInMinutes,
                    MessageReadThreshold = messageReadThreshold
                  },
                  QueueUrlInfo = new Domain.SqsListener.QueueUrlInfo
                  {
                    AdapterNotificationQueueUrl = queueInfoVault.AdapterNotificationQueueUrl,
                    PushDataQueueUrl = queueInfoVault.PushDataQueueUrl
                  }
                };
              });

              services.AddSingleton(s =>
              {
                int.TryParse(queueInfoVault.RecieveMessagesMaxCount, out int recieveMessagesMaxCount);
                recieveMessagesMaxCount = recieveMessagesMaxCount == 0 ? 10 : recieveMessagesMaxCount;

                int.TryParse(queueInfoVault.RecieveWaitTimeInSeconds, out int recieveWaitTimeInSeconds); // Default value 0

                var sqsConfiguration = new SqsConfiguration
                {
                  ServiceUrl = queueInfoVault.ServiceUrl,
                  AccessKeyId = queueInfoVault.AccessKeyId,
                  AccessSecretKey = queueInfoVault.AccessSecretKey,
                  RecieveMessagesMaxCount = recieveMessagesMaxCount,
                  RecieveWaitTimeInSeconds = recieveWaitTimeInSeconds
                };

                return sqsConfiguration;
              });
              services.AddSingleton<IAwsSqsService, AwsSqsService>();

              services.AddHttpClient("AdaptorApi", c =>
              {
                bool.TryParse(_isApiGatewayEnabled, out bool isApiGatewayEnabled);
                c.BaseAddress = new Uri(isApiGatewayEnabled ? adaptorApiSettings.ApiGatewayEnabledUrl : adaptorApiSettings.ApiGatewayDisabledUrl);
                c.DefaultRequestHeaders.Add("X-API-Key", adaptorApiSettings.ApiKey);
              });
              services.AddHttpClient("ConsumerClient");
              services.AddHostedService<WrapperNotificationListner>();
              services.AddHostedService<AdapterPushDataListner>();
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
      var _secrets = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/adaptor-sqs-listener", mountPathValue);
      return _secrets.Data;
    }
  }
}
