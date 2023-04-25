using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace CcsSso.Core.BSIRolesRemovalOneTimeJob
{
  public class ProgramHelpers
  {
    private readonly IAwsParameterStoreService _awsParameterStoreService;
    private static string path = "/conclave-sso/bsi-role-removal-job/";

    public ProgramHelpers()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
    }


    public dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(WrapperApiSettings))
      {
        returnParams = new WrapperApiSettings()
        {
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/Url"),
          ApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiKey"),
        };
      }

      else if (objType == typeof(ScheduleJobSettings))
      {
        returnParams = new ScheduleJobSettings()
        {
          OrganisationAutovalidationJobExecutionFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "ScheduleJobSettings/OrganisationAutovalidationJobExecutionFrequencyInMinutes"))
        };
      }
      else if (objType == typeof(OrgAutoValidationOneTimeJob))
      {
        returnParams = new OrgAutoValidationOneTimeJob()
        {
          Enable = Convert.ToBoolean(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/Enable")),
          StartDate = _awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/StartDate"),
          EndDate = _awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJob/EndDate")

        };
      }
      else if (objType == typeof(OrgAutoValidationOneTimeJobRoles))
      {
        returnParams = new OrgAutoValidationOneTimeJobRoles()
        {
          RemoveRoleFromAllOrg = getStringToArray(_awsParameterStoreService.FindParameterByName(parameters, path + "OrgAutoValidationOneTimeJobRoles/RemoveRoleFromAllOrg")),
        };
      }

      return returnParams;
    }

    private string[] getStringToArray(string param)
    {
      if (param != null)
      {
        return param.Split(',').ToArray();
      }
      return Array.Empty<string>();
    }

    public async Task<List<Parameter>> LoadAwsSecretsAsync(IAwsParameterStoreService _awsParameterStoreService)
    {
      return await _awsParameterStoreService.GetParameters(path);
    }

    public async Task<Dictionary<string, object>> LoadSecretsAsync()
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
      var _secrets = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync("secret/bsi-role-removal-job", mountPathValue);
      return _secrets.Data;
    }
  }
}