using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DormancyJobScheduler.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;

namespace CcsSso.Core.DormancyJobScheduler
{
  public class ProgramHelpers
  {
    private readonly IAwsParameterStoreService _awsParameterStoreService;
    private static string path = "/conclave-sso/dormancy-job/";

    public ProgramHelpers()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
    }

    public dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(DormancyJobSettings))
      {
        returnParams = new DormancyJobSettings()
        {
          DelayInMilliSeconds = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/DelayInMilliSeconds")),
          DeactivationNotificationInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/DeactivationNotificationInMinutes")),
          DormancyNotificationJobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/DormancyNotificationJobFrequencyInMinutes")),
          UserDeactivationDurationInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/UserDeactivationDurationInMinutes")),
          UserDeactivationJobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/UserDeactivationJobFrequencyInMinutes")),
          DormancyNotificationJobEnable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/DormancyNotificationJobEnable")),
          UserDeactivationJobEnable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/UserDeactivationJobEnable")),
          ArchivalJobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/ArchivalJobFrequencyInMinutes")),
          AdminDormantedUserArchivalDurationInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/AdminDormantedUserArchivalDurationInMinutes")),
          JobDormantedUserArchivalDurationInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/JobDormantedUserArchivalDurationInMinutes")),
          UserArchivalJobEnable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "DormancyJobSettings/UserArchivalJobEnable")),
        };
      }
      return returnParams;
    }

		public dynamic FillWrapperApiSettingsAwsParamsValue(Type objType, List<Parameter> parameters)
		{
			dynamic? returnParams = null;

			if (objType == typeof(WrapperApiSettings))
			{
				returnParams = new WrapperApiSettings()
				{
					UserApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/UserApiKey"),
					ApiGatewayEnabledUserUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayEnabledUserUrl"),
					ApiGatewayDisabledUserUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayDisabledUserUrl"),
				};
			}
			return returnParams;
		}
    public dynamic FillSecuritySettingsAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(SecurityApiSettings))
      {
        returnParams = new SecurityApiSettings()
        {
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/ApiKey"),
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "SecurityApiSettings/Url"),
        };
      }
      return returnParams;
    }
    public dynamic FillEmailSettingsAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(EmailSettings))
      {
        returnParams = new EmailSettings()
        {
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "EmailSettings/ApiKey"),
          UserDormantNotificationTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "EmailSettings/UserDormantNotificationTemplateId"),
        };
      }
      return returnParams;
    }
    public dynamic FillNotificationApiSettingsAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(NotificationApiSettings))
      {
        returnParams = new NotificationApiSettings()
        {
          NotificationApiUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "NotificationApiSettings/NotificationApiUrl"),
          NotificationApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "NotificationApiSettings/NotificationApiKey"),
        };
      }
      return returnParams;
    }
    public dynamic FillTestModeSettingsAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(TestModeSettings))
      {
        returnParams = new TestModeSettings()
        {
          Enable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "TestModeSettings/Enable")),
          Keyword = _awsParameterStoreService.FindParameterByName(parameters, path + "TestModeSettings/Keyword"),
        };
      }
      return returnParams;
    }

    public async Task<List<Parameter>> LoadAwsSecretsAsync(IAwsParameterStoreService _awsParameterStoreService)
    {
      return await _awsParameterStoreService.GetParameters(path);
    }
  }
}
