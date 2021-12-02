using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Contracts
{
  public interface IAwsS3Service
  {
    Task WritingAnObjectAsync(string fileKey, string contentType, string bucketName, Stream fileContent);

    string GeneratePreSignedURL(string fileKey, string bucketName, int duration);

    Task<string> ReadObjectDataStringAsync(string fileKey, string bucketName);
  }
}
