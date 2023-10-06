using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DataMigrationJobScheduler.Contracts
{
  public interface IAwsS3Service
  {
    Task WritingAnObjectAsync(string fileKey, string contentType, string bucketName, Stream fileContent);

    string GeneratePreSignedURL(string fileKey, string bucketName, int duration);

    Task<string> ReadObjectDataStringAsync(string fileKey, string bucketName);
    Task<byte[]> ReadObjectData(string fileKey, string bucketName);
    Task WritingAFileDataAsync(string fileKey, string contentType, string bucketName, string fileContent);
  }
}
