using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DelegationJobScheduler.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;

namespace CcsSso.Core.DelegationJobScheduler
{
  public class ProgramHelpers
  {
    private readonly IAwsParameterStoreService _awsParameterStoreService;
    private static string path = "/conclave-sso/delegation-job/";

    public ProgramHelpers()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
    }

    public dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(DelegationJobSettings))
      {
        returnParams = new DelegationJobSettings()
        {
          DelegationLinkExpiryJobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DelegationJobSettings/DelegationLinkExpiryJobFrequencyInMinutes")),
          DelegationTerminationJobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DelegationJobSettings/DelegationTerminationJobFrequencyInMinutes"))
        };
      }
      return returnParams;
    }

    public dynamic FillExpiryNotificationAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(DelegationExpiryNotificationJobSettings))
      {
        returnParams = new DelegationExpiryNotificationJobSettings()
        {
          JobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DelegationExpiryNotificationJobSettings/JobFrequencyInMinutes")),
          ExpiryNoticeInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DelegationExpiryNotificationJobSettings/ExpiryNoticeInMinutes"))
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
          DelegationExpiryNotificationToUserTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "EmailSettings/DelegationExpiryNotificationToUserTemplateId"),
          DelegationExpiryNotificationToAdminTemplateId = _awsParameterStoreService.FindParameterByName(parameters, path + "EmailSettings/DelegationExpiryNotificationToAdminTemplateId"),
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
