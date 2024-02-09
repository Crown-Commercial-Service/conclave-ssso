
namespace CcsSso.Core.DataMigrationJobScheduler.Model
{
  public class DataMigrationAppSettings
  {
    public DataMigrationJobSettings DataMigrationJobSettings { get; set; }
    public WrapperApiSettings WrapperApiSettings { get; set; }
    public bool IsApiGatewayEnabled { get; set; }
    public DataMigrationAPI DataMigrationAPI { get; set; }
    public S3ConfigurationInfo S3configInfo { get; set; }
	}

	public class WrapperApiSettings
  {
		public string OrgApiKey { get; set; }
		public string ApiGatewayEnabledOrgUrl { get; set; }
		public string ApiGatewayDisabledOrgUrl { get; set; }
	}

  public class DataMigrationJobSettings
  {
    public int DataMigrationFileUploadJobFrequencyInMinutes { get; set; }
  }

  public class DataMigrationAPI
  {
    public string Url { get; set; }
    public string Token { get; set; }
  }

  public class S3ConfigurationInfo
  {
    public string ServiceUrl { get; set; }

    public string AccessKeyId { get; set; }

    public string AccessSecretKey { get; set; }

    public string DataMigrationBucketName { get; set; }

    public string DataMigrationTemplateFolderName { get; set; }

    public string DataMigrationFolderName { get; set; }
    public string DataMigrationSuccessFolderName { get; set; }
    public string DataMigrationFailedFolderName { get; set; }

    public int FileAccessExpirationInHours { get; set; }
  }
}
