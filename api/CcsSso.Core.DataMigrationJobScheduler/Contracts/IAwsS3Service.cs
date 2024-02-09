
namespace CcsSso.Core.DataMigrationJobScheduler.Contracts
{
  public interface IAwsS3Service
  {
    Task<string> ReadObjectDataStringAsync(string fileKey, string bucketName);

    Task<byte[]> ReadObjectData(string fileKey, string bucketName);
    
    Task WritingAFileDataAsync(string fileKey, string contentType, string bucketName, string fileContent);
  }
}
