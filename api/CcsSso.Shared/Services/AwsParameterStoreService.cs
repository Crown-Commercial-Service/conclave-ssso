using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class AwsParameterStoreService : IAwsParameterStoreService
  {
    private AmazonSimpleSystemsManagementClient _client;
    public AmazonSimpleSystemsManagementSettings _settings;

    public AwsParameterStoreService()
    {
      Console.WriteLine("AwsParameterStoreService");
      string env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      Console.WriteLine("env" + env);
      var envData = (JObject)JsonConvert.DeserializeObject(env);
      Console.WriteLine("envData" + envData);
      string setting = JsonConvert.SerializeObject(envData["user-provided"].FirstOrDefault(obj => obj["name"].Value<string>().Contains("ssm-service")));
      Console.WriteLine("setting" + setting);
      _settings = JsonConvert.DeserializeObject<AmazonSimpleSystemsManagementSettings>(setting.ToString());

      Console.WriteLine("setting_aws_access_key_id" + _settings.credentials.aws_access_key_id);
      Console.WriteLine("setting_aws_secret_access_key" + _settings.credentials.aws_secret_access_key);
      Console.WriteLine("setting_region" + _settings.credentials.region);

      var credentials = new BasicAWSCredentials(_settings.credentials.aws_access_key_id, _settings.credentials.aws_secret_access_key);
      _client = new AmazonSimpleSystemsManagementClient(credentials, RegionEndpoint.GetBySystemName(_settings.credentials.region));
    }

    public async Task<List<Parameter>> GetParameters(string path)
    {
      List<Parameter> parameters = new List<Parameter>();
      string nextToken = default;
      do
      {
        // Query AWS Parameter Store
        var response = await _client.GetParametersByPathAsync(
            new GetParametersByPathRequest
            {
              Path = path,
              WithDecryption = true,
              Recursive = true,
              NextToken = nextToken
            });

        parameters.AddRange(response.Parameters);

        // Possibly get more
        nextToken = response.NextToken;
      } while (!String.IsNullOrEmpty(nextToken));

      return parameters;
    }

    public string FindParameterByName(List<Parameter> parameters, string name)
    {
      string value = string.Empty;
      var parameter = parameters.FirstOrDefault(p => p.Name == name);
      if (parameter != null)
      {
        value = Convert.ToString(parameter.Value);
      }
      return value;
    }
  }
}
