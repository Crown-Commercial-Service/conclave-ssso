using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Adaptor.Domain.SqsListener;
using CcsSso.Adaptor.SqsListener.Listners;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Helpers;
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
              SecurityApiSettingsVault securityApiSettingsVault;
              DataQueueSettingsVault dataQueueSettingsVault;
              EmailSettingsVault emailSettingsVault;
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
                  securityApiSettingsVault = (SecurityApiSettingsVault)FillAwsParamsValue(typeof(SecurityApiSettingsVault), parameters);
                  dataQueueSettingsVault = (DataQueueSettingsVault)FillAwsParamsValue(typeof(DataQueueSettingsVault), parameters);
                  emailSettingsVault = (EmailSettingsVault)FillAwsParamsValue(typeof(EmailSettingsVault), parameters);
                }
                else
                {
                  var secrets = LoadSecretsAsync().Result;
                  _isApiGatewayEnabled = secrets["IsApiGatewayEnabled"].ToString();
                  adaptorApiSettings = JsonConvert.DeserializeObject<AdaptorApiSetting>(secrets["AdaptorApiSettings"].ToString());
                  sqsJobSettingsVault = JsonConvert.DeserializeObject<SqsListnerJobSettingVault>(secrets["SqsListnerJobSettings"].ToString());
                  queueInfoVault = JsonConvert.DeserializeObject<QueueInfoVault>(secrets["QueueInfo"].ToString());
                  securityApiSettingsVault = JsonConvert.DeserializeObject<SecurityApiSettingsVault>(secrets["SecurityApiSettings"].ToString());
                  dataQueueSettingsVault = JsonConvert.DeserializeObject<DataQueueSettingsVault>(secrets["DataQueueSettings"].ToString());
                  emailSettingsVault = JsonConvert.DeserializeObject<EmailSettingsVault>(secrets["Email"].ToString());
                }
              }
              else
              {
                var config = hostContext.Configuration;
                _isApiGatewayEnabled = config["IsApiGatewayEnabled"];
                adaptorApiSettings = config.GetSection("AdaptorApiSettings").Get<AdaptorApiSetting>();
                sqsJobSettingsVault = config.GetSection("SqsListnerJobSettings").Get<SqsListnerJobSettingVault>();
                queueInfoVault = config.GetSection("QueueInfo").Get<QueueInfoVault>();
                securityApiSettingsVault = config.GetSection("SecurityApiSettings").Get<SecurityApiSettingsVault>();
                dataQueueSettingsVault = config.GetSection("DataQueueSettings").Get<DataQueueSettingsVault>();
                emailSettingsVault = config.GetSection("Email").Get<EmailSettingsVault>();
              }

              services.AddSingleton(s =>
              {
                int.TryParse(sqsJobSettingsVault.JobSchedulerExecutionFrequencyInMinutes, out int jobSchedulerExecutionFrequencyInMinutes);
                int.TryParse(sqsJobSettingsVault.MessageReadThreshold, out int messageReadThreshold);

                if (jobSchedulerExecutionFrequencyInMinutes == 0)
                {
                  jobSchedulerExecutionFrequencyInMinutes = 10;
                }

                int.TryParse(sqsJobSettingsVault.DataQueueJobSchedulerExecutionFrequencyInMinutes, out int dataQueueJobSchedulerExecutionFrequencyInMinutes);
                int.TryParse(sqsJobSettingsVault.DataQueueMessageReadThreshold, out int dataQueueMessageReadThreshold);

                if (jobSchedulerExecutionFrequencyInMinutes == 0)
                {
                  jobSchedulerExecutionFrequencyInMinutes = 10;
                }


                int.TryParse(dataQueueSettingsVault.DelayInSeconds, out int dataQueueSettingsDelayInSeconds);
                if (dataQueueSettingsDelayInSeconds == 0)
                {
                  dataQueueSettingsDelayInSeconds = 2;
                }

                int.TryParse(dataQueueSettingsVault.RetryMaxCount, out int dataQueueSettingsRetryMaxCount);
                if (dataQueueSettingsRetryMaxCount == 0)
                {
                  dataQueueSettingsRetryMaxCount = 2;
                }

                bool.TryParse(emailSettingsVault.SendNotificationsEnabled, out bool emailSettingSendNotificationsEnabled);

                return new SqsListnerAppSetting
                {
                  SqsListnerJobSetting = new SqsListnerJobSetting
                  {
                    JobSchedulerExecutionFrequencyInMinutes = jobSchedulerExecutionFrequencyInMinutes,
                    MessageReadThreshold = messageReadThreshold,
                    DataQueueJobSchedulerExecutionFrequencyInMinutes = dataQueueJobSchedulerExecutionFrequencyInMinutes,
                    DataQueueMessageReadThreshold = dataQueueMessageReadThreshold
                  },
                  QueueUrlInfo = new Domain.SqsListener.QueueUrlInfo
                  {
                    AdaptorNotificationQueueUrl = queueInfoVault.AdaptorNotificationQueueUrl,
                    PushDataQueueUrl = queueInfoVault.PushDataQueueUrl,
                    DataQueueUrl = queueInfoVault.DataQueueUrl
                  },
                  DataQueueSettings = new Domain.SqsListener.DataQueueSettings
                  {
                    DelayInSeconds = dataQueueSettingsDelayInSeconds,
                    RetryMaxCount = dataQueueSettingsRetryMaxCount
                  },
                  SecurityApiSettings = new Domain.SqsListener.SecurityApiSettings
                  {
                    ApiKey = securityApiSettingsVault.ApiKey,
                    Url = securityApiSettingsVault.Url
                  },
                  EmailSettings = new Domain.SqsListener.EmailSettings
                  {
                    ApiKey = emailSettingsVault.ApiKey,
                    SendNotificationsEnabled = emailSettingSendNotificationsEnabled,
                    Auth0CreateUserErrorNotificationTemplateId = emailSettingsVault.Auth0CreateUserErrorNotificationTemplateId,
                    Auth0DeleteUserErrorNotificationTemplateId = emailSettingsVault.Auth0DeleteUserErrorNotificationTemplateId,
                    SendDataQueueErrorNotificationToEmailIds = emailSettingsVault.SendDataQueueErrorNotificationToEmailIds,
                  }
                };
              });

              services.AddSingleton(s =>
              {
                int.TryParse(queueInfoVault.RecieveMessagesMaxCount, out int recieveMessagesMaxCount);
                recieveMessagesMaxCount = recieveMessagesMaxCount == 0 ? 10 : recieveMessagesMaxCount;

                int.TryParse(queueInfoVault.RecieveWaitTimeInSeconds, out int recieveWaitTimeInSeconds); // Default value 0

                int.TryParse(queueInfoVault.DataQueueRecieveMessagesMaxCount, out int dataQueueRecieveMessagesMaxCount);
                dataQueueRecieveMessagesMaxCount = dataQueueRecieveMessagesMaxCount == 0 ? 10 : dataQueueRecieveMessagesMaxCount;

                int.TryParse(queueInfoVault.DataQueueRecieveWaitTimeInSeconds, out int dataQueueRecieveWaitTimeInSeconds); // Default value 0

                var sqsConfiguration = new SqsConfiguration
                {
                  ServiceUrl = queueInfoVault.ServiceUrl,
                  AccessKeyId = queueInfoVault.AdaptorNotificationAccessKeyId,
                  AccessSecretKey = queueInfoVault.AdaptorNotificationAccessSecretKey,
                  PushDataAccessKeyId = queueInfoVault.PushDataAccessKeyId,
                  PushDataAccessSecretKey = queueInfoVault.PushDataAccessSecretKey,
                  DataQueueAccessKeyId = queueInfoVault.DataQueueAccessKeyId,
                  DataQueueAccessSecretKey = queueInfoVault.DataQueueAccessSecretKey,
                  DataQueueRecieveMessagesMaxCount = dataQueueRecieveMessagesMaxCount,
                  DataQueueRecieveWaitTimeInSeconds = dataQueueRecieveWaitTimeInSeconds,
                  RecieveMessagesMaxCount = recieveMessagesMaxCount,
                  RecieveWaitTimeInSeconds = recieveWaitTimeInSeconds
                };

                return sqsConfiguration;
              });

              services.AddSingleton(s =>
              {
                EmailConfigurationInfo emailConfigurationInfo = new()
                {
                  ApiKey = emailSettingsVault.ApiKey,
                };

                return emailConfigurationInfo;
              });

              services.AddSingleton<IAwsSqsService, AwsSqsService>();
              services.AddSingleton<IAwsPushDataSqsService, AwsPushDataSqsService>();
              services.AddSingleton<IAwsDataSqsService, AwsDataSqsService>();
              services.AddSingleton<IEmailProviderService, EmailProviderService>();

              services.AddHttpClient("AdaptorApi", c =>
              {
                bool.TryParse(_isApiGatewayEnabled, out bool isApiGatewayEnabled);
                c.BaseAddress = new Uri(isApiGatewayEnabled ? adaptorApiSettings.ApiGatewayEnabledUrl : adaptorApiSettings.ApiGatewayDisabledUrl);
                c.DefaultRequestHeaders.Add("X-API-Key", adaptorApiSettings.ApiKey);
              });

              services.AddHttpClient("SecurityApi", c =>
              {
                c.BaseAddress = new Uri(securityApiSettingsVault.Url);
                c.DefaultRequestHeaders.Add("X-API-Key", securityApiSettingsVault.ApiKey);
              });

              services.AddHttpClient("ConsumerClient");

              services.AddHostedService<WrapperNotificationListner>();
              services.AddHostedService<AdapterPushDataListner>();
              services.AddHostedService<DataQueueListner>();
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
      if (objType == typeof(AdaptorApiSetting))
      {
        returnParams = new AdaptorApiSetting()
        {
          ApiGatewayEnabledUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "AdaptorApiSettings/ApiGatewayEnabledUrl"),
          ApiGatewayDisabledUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "AdaptorApiSettings/ApiGatewayDisabledUrl"),
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "AdaptorApiSettings/ApiKey"),
        };
      }
      else if (objType == typeof(SqsListnerJobSettingVault))
      {
        returnParams = new SqsListnerJobSettingVault()
        {
          JobSchedulerExecutionFrequencyInMinutes = _awsParameterStoreService.FindParameterByName(parameters, path + "SqsListnerJobSettings/JobSchedulerExecutionFrequencyInMinutes"),
          MessageReadThreshold = _awsParameterStoreService.FindParameterByName(parameters, path + "SqsListnerJobSettings/MessageReadThreshold"),
          DataQueueJobSchedulerExecutionFrequencyInMinutes = _awsParameterStoreService.FindParameterByName(parameters, path + "SqsListnerJobSettings/DataQueueJobSchedulerExecutionFrequencyInMinutes"),
          DataQueueMessageReadThreshold = _awsParameterStoreService.FindParameterByName(parameters, path + "SqsListnerJobSettings/DataQueueMessageReadThreshold")
        };
      }
      else if (objType == typeof(QueueInfoVault))
      {
        string AdaptorNotificationAccessKeyId;
        string AdaptorNotificationAccessSecretKey;
        string AdaptorNotificationQueueUrl;

        string PushDataQueueUrl;
        string PushDataAccessKeyId;
        string PushDataAccessSecretKey;

        string AccessKeyId;
        string AccessSecretKey;

        string DataQueueUrl;
        string DataQueueAccessKeyId;
        string DataQueueAccessSecretKey;

        var queueInfoAdaptorNotificationName = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationName"); // AdaptorNotification

        if (!string.IsNullOrEmpty(queueInfoAdaptorNotificationName))
        {
          var queueInfo = UtilityHelper.GetSqsSetting(queueInfoAdaptorNotificationName);
          AdaptorNotificationAccessKeyId = queueInfo.credentials.aws_access_key_id;
          AdaptorNotificationAccessSecretKey = queueInfo.credentials.aws_secret_access_key;
          AdaptorNotificationQueueUrl = queueInfo.credentials.primary_queue_url;
          AccessKeyId = queueInfo.credentials.aws_access_key_id; // this is not used in sqs listener. todo: Need to refactor
          AccessSecretKey = queueInfo.credentials.aws_secret_access_key; // this is not used in sqs listener. todo: Need to refactor
        }
        else
        {
          AdaptorNotificationAccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationAccessKeyId");
          AdaptorNotificationAccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationAccessSecretKey");
          AdaptorNotificationQueueUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AdaptorNotificationQueueUrl");
          AccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AccessKeyId"); // this is not used in sqs listener. todo: Need to refactor
          AccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/AccessSecretKey"); // this is not used in sqs listener. todo: Need to refactor
        }

        var queuePushDataNotificationName = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataName"); // PushNotification

        if (!string.IsNullOrEmpty(queuePushDataNotificationName))
        {
          var queueInfo = UtilityHelper.GetSqsSetting(queuePushDataNotificationName);
          PushDataAccessKeyId = queueInfo.credentials.aws_access_key_id;
          PushDataAccessSecretKey = queueInfo.credentials.aws_secret_access_key;
          PushDataQueueUrl = queueInfo.credentials.primary_queue_url;
        }
        else
        {
          PushDataAccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataAccessKeyId");
          PushDataAccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataAccessSecretKey");
          PushDataQueueUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/PushDataQueueUrl");
        }

        var queueDataName = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataName"); // Data Queue

        if (!string.IsNullOrEmpty(queueDataName))
        {
          var queueInfo = UtilityHelper.GetSqsSetting(queueDataName);
          DataQueueAccessKeyId = queueInfo.credentials.aws_access_key_id;
          DataQueueAccessSecretKey = queueInfo.credentials.aws_secret_access_key;
          DataQueueUrl = queueInfo.credentials.primary_queue_url;
        }
        else
        {
          DataQueueAccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueAccessKeyId");
          DataQueueAccessSecretKey = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueAccessSecretKey");
          DataQueueUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueUrl");
        }

        returnParams = new QueueInfoVault()
        {
          AccessKeyId = AccessKeyId,
          AccessSecretKey = AccessSecretKey,
          ServiceUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/ServiceUrl"),

          RecieveMessagesMaxCount = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/RecieveMessagesMaxCount"),
          RecieveWaitTimeInSeconds = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/RecieveWaitTimeInSeconds"),

          AdaptorNotificationQueueUrl = AdaptorNotificationQueueUrl,
          AdaptorNotificationAccessKeyId = AdaptorNotificationAccessKeyId,
          AdaptorNotificationAccessSecretKey = AdaptorNotificationAccessSecretKey,

          PushDataQueueUrl = PushDataQueueUrl,
          PushDataAccessKeyId = PushDataAccessKeyId,
          PushDataAccessSecretKey = PushDataAccessSecretKey,

          DataQueueUrl = DataQueueUrl,
          DataQueueAccessKeyId = DataQueueAccessKeyId,
          DataQueueAccessSecretKey = DataQueueAccessSecretKey,
          DataQueueRecieveMessagesMaxCount = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueRecieveMessagesMaxCount"),
          DataQueueRecieveWaitTimeInSeconds = _awsParameterStoreService.FindParameterByName(parameters, path + "QueueInfo/DataQueueRecieveWaitTimeInSeconds"),

        };
      }
      else if (objType == typeof(DataQueueSettingsVault))
      {
        returnParams = new DataQueueSettingsVault()
        {
          DelayInSeconds = _awsParameterStoreService.FindParameterByName(parameters, path + "DataQueueSettings/DelayInSeconds"),
          RetryMaxCount = _awsParameterStoreService.FindParameterByName(parameters, path + "DataQueueSettings/RetryMaxCount")
        };
      }
      else if (objType == typeof(SecurityApiSettingsVault))
      {
        returnParams = new SecurityApiSettingsVault()
        {
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/ApiKey"),
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/Url")
        };
      }
      else if (objType == typeof(EmailSettingsVault))
      {
        returnParams = new EmailSettingsVault()
        {
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/ApiKey"),
          SendNotificationsEnabled = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/SendNotificationsEnabled"),
          Auth0CreateUserErrorNotificationTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/Auth0CreateUserErrorNotificationTemplateId"),
          Auth0DeleteUserErrorNotificationTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "Email/Auth0DeleteUserErrorNotificationTemplateId"),
          SendDataQueueErrorNotificationToEmailIds = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "Email/SendDataQueueErrorNotificationToEmailIds"))
        };
      }
      return returnParams;
    }

    private static string[] getStringToArray(string param)
    {
      if (param != null)
      {
        return param.Split(',').ToArray();
      }
      return Array.Empty<string>();
    }
  }
}
