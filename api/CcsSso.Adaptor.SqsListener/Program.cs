using Amazon.SimpleSystemsManagement.Model;
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
    private static string vaultSource;
    private static string path = "/conclave-sso/adaptor-sqs-listener/";
    private static IAwsParameterStoreService _awsParameterStoreService;

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
              vaultSource = configBuilder.GetValue<string>("Source");
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
                if (vaultSource?.ToUpper() == "AWS")
                {
                  var parameters = LoadAwsSecretsAsync().Result;
                  _isApiGatewayEnabled = _awsParameterStoreService.FindParameterByName(parameters, path + "IsApiGatewayEnabled");
                  adaptorApiSettings = (AdaptorApiSetting)FillAwsParamsValue(typeof(AdaptorApiSetting), parameters);
                  sqsJobSettingsVault = (SqsListnerJobSettingVault)FillAwsParamsValue(typeof(SqsListnerJobSettingVault), parameters);
                  queueInfoVault = (QueueInfoVault)FillAwsParamsValue(typeof(QueueInfoVault), parameters);
                }
                else
                {
                  var secrets = LoadSecretsAsync().Result;
                  _isApiGatewayEnabled = secrets["IsApiGatewayEnabled"].ToString();
                  adaptorApiSettings = JsonConvert.DeserializeObject<AdaptorApiSetting>(secrets["AdaptorApiSettings"].ToString());
                  sqsJobSettingsVault = JsonConvert.DeserializeObject<SqsListnerJobSettingVault>(secrets["SqsListnerJobSettings"].ToString());
                  queueInfoVault = JsonConvert.DeserializeObject<QueueInfoVault>(secrets["QueueInfo"].ToString());
                }
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
                    AdaptorNotificationQueueUrl = queueInfoVault.AdaptorNotificationQueueUrl,
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
                  AccessKeyId = queueInfoVault.AdaptorNotificationAccessKeyId,
                  AccessSecretKey = queueInfoVault.AdaptorNotificationAccessSecretKey,
                  PushDataAccessKeyId = queueInfoVault.PushDataAccessKeyId,
                  PushDataAccessSecretKey = queueInfoVault.PushDataAccessSecretKey,
                  RecieveMessagesMaxCount = recieveMessagesMaxCount,
                  RecieveWaitTimeInSeconds = recieveWaitTimeInSeconds
                };

                return sqsConfiguration;
              });

              services.AddSingleton<IAwsSqsService, AwsSqsService>();
              services.AddSingleton<IAwsPushDataSqsService, AwsPushDataSqsService>();

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

    private static async Task<List<Parameter>> LoadAwsSecretsAsync()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
      return await _awsParameterStoreService.GetParameters(path);
    }

    private static dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;
      if (objType  == typeof(AdaptorApiSetting))
      {
        returnParams = new AdaptorApiSetting()
        {
          ApiGatewayEnabledUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "AdaptorApiSettings/ApiGatewayEnabledUrl"),
          ApiGatewayDisabledUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "AdaptorApiSettings/ApiGatewayDisabledUrl"),
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "AdaptorApiSettings/ApiKey"),
        };
      }
      else if (objType  == typeof(SqsListnerJobSettingVault))
      {
        returnParams = new SqsListnerJobSettingVault()
        {
          JobSchedulerExecutionFrequencyInMinutes = _awsParameterStoreService.FindParameterByName(parameters, path + "SqsListnerJobSettings/JobSchedulerExecutionFrequencyInMinutes"),
          MessageReadThreshold = _awsParameterStoreService.FindParameterByName(parameters, path + "SqsListnerJobSettings/MessageReadThreshold")
        };
      }
      else if (objType  == typeof(QueueInfoVault))
      {
        returnParams = new QueueInfoVault()
        {
          AccessKeyId   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AccessKeyId"),
          AccessSecretKey   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AccessSecretKey"),
          ServiceUrl   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/ServiceUrl"),
          RecieveMessagesMaxCount   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/RecieveMessagesMaxCount"),
          RecieveWaitTimeInSeconds   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/RecieveWaitTimeInSeconds"),
          AdaptorNotificationQueueUrl   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationQueueUrl"),
          PushDataQueueUrl   = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataQueueUrl"),
          AdaptorNotificationAccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationAccessKeyId"),
          AdaptorNotificationAccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationAccessSecretKey"),
          PushDataAccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataAccessKeyId"),
          PushDataAccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataAccessSecretKey")
        };
      }
      return returnParams;
    }

  }
}
