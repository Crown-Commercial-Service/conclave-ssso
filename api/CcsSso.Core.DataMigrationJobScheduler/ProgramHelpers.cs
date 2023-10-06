using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Services;

namespace CcsSso.Core.DataMigrationJobScheduler
{
  public class ProgramHelpers
  {
    private readonly IAwsParameterStoreService _awsParameterStoreService;
    private static string path = "/conclave-sso/data-migration-job/";

    public ProgramHelpers()
    {
      _awsParameterStoreService = new AwsParameterStoreService();
    }

    public dynamic FillAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(DataMigrationJobSettings))
      {
        returnParams = new DataMigrationJobSettings()
        {
          DataMigrationFileUploadJobFrequencyInMinutes = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "DataMigrationJobSettings/DataMigrationFileUploadJobFrequencyInMinutes"))
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
					OrgApiKey = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/OrgApiKey"),
					ApiGatewayEnabledOrgUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayEnabledOrgUrl"),
					ApiGatewayDisabledOrgUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "WrapperApiSettings/ApiGatewayDisabledOrgUrl"),
				};
			}
			return returnParams;
		}
    public dynamic FillCiiApiAwsParamsValue(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(DataMigrationAPI))
      {
        returnParams = new DataMigrationAPI()
        {
          Url = _awsParameterStoreService.FindParameterByName(parameters, path + "DataMigrationAPI/Url"),
          Token = _awsParameterStoreService.FindParameterByName(parameters, path + "DataMigrationAPI/Token"),
        };
      }
      return returnParams;
    }

    public dynamic FillS3ConfigInfo(Type objType, List<Parameter> parameters)
    {
      dynamic? returnParams = null;

      if (objType == typeof(S3ConfigurationInfo))
      {
        returnParams = new S3ConfigurationInfo()
        {
          ServiceUrl = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/ServiceUrl"),
          AccessKeyId = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/AccessKeyId"),
          AccessSecretKey=_awsParameterStoreService.FindParameterByName(parameters,path+ "S3ConfigurationInfo/AccessSecretKey"),
          DataMigrationBucketName= _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/DataMigrationBucketName"),
          DataMigrationTemplateFolderName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/DataMigrationTemplateFolderName"),
          DataMigrationFolderName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/DataMigrationFolderName"),
          DataMigrationSuccessFolderName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/DataMigrationSuccessFolderName"),
          DataMigrationFailedFolderName = _awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/DataMigrationFailedFolderName"),
          FileAccessExpirationInHours = Convert.ToInt32(_awsParameterStoreService.FindParameterByName(parameters, path + "S3ConfigurationInfo/FileAccessExpirationInHours"))
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
