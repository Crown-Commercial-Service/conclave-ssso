using Amazon.S3;
using Amazon.S3.Model;
using CcsSso.Core.DataMigrationJobScheduler.Contracts;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using Microsoft.Extensions.Logging;

namespace CcsSso.Core.DataMigrationJobScheduler.Services
{
    public class AwsS3Service : IAwsS3Service
    {
      private readonly IAmazonS3 _client;
      private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly ILogger<IAwsS3Service> _logger;

    public AwsS3Service(S3ConfigurationInfo s3ConfigurationInfo, ILogger<IAwsS3Service> logger)
      {
      _logger = logger;
      _s3ConfigurationInfo = s3ConfigurationInfo;
       AmazonS3Config amazonS3Config = new AmazonS3Config
       {
          ServiceURL = _s3ConfigurationInfo.ServiceUrl
       };
      _client = new AmazonS3Client(_s3ConfigurationInfo.AccessKeyId, _s3ConfigurationInfo.AccessSecretKey, amazonS3Config);
      }

      /// <summary>
      /// Read the file object as string
      /// </summary>
      /// <param name="fileKey"></param>
      /// <param name="bucketName"></param>
      /// <returns></returns>
      public async Task<string> ReadObjectDataStringAsync(string fileKey, string bucketName)
      {
      try
      {
        string responseBody = "";
        GetObjectRequest request = new GetObjectRequest
        {
          BucketName = bucketName,
          Key = fileKey
        };
        using (GetObjectResponse response = await _client.GetObjectAsync(request))
        using (Stream responseStream = response.ResponseStream)
        using (StreamReader reader = new StreamReader(responseStream))
        {
          string title = response.Metadata["x-amz-meta-title"]; // Assume you have "title" as medata added to the object.
          string contentType = response.Headers["Content-Type"];
          Console.WriteLine("Object metadata, Title: {0}", title);
          Console.WriteLine("Content type: {0}", contentType);

          responseBody = reader.ReadToEnd(); // Now you process the response body.
        }
        return responseBody;
      }
      catch(Exception ex)
      {
        _logger.LogError($"Error Reading File String Data from S3 -{ex.Message}");
        throw ex;
      }
      }

    public async Task<byte[]> ReadObjectData(string fileKey, string bucketName)
    {
      try
      {
        string responseBody = "";
        GetObjectRequest request = new GetObjectRequest
        {
          BucketName = bucketName,
          Key = fileKey
        };
        using (GetObjectResponse response = await _client.GetObjectAsync(request))
        using (Stream responseStream = response.ResponseStream)
        using (StreamReader reader = new StreamReader(responseStream))
        using (var memoryStream = new MemoryStream())
        {
          await responseStream.CopyToAsync(memoryStream);
          return memoryStream.ToArray();
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"Error Reading File Object Data from S3 -{ex.Message}");
        throw ex;
      }
    }
    /// <summary>
    /// Write a file Data
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="contentType"></param>
    /// <param name="bucketName"></param>
    /// <param name="fileContent"></param>
    /// <returns></returns>
    public async Task WritingAFileDataAsync(string fileKey, string contentType, string bucketName, string fileContent)
    {
      try
      {
        var putRequest1 = new PutObjectRequest
        {
          BucketName = bucketName,
          Key = fileKey,
          ContentBody = fileContent,
          ContentType = contentType
        };

        PutObjectResponse response = await _client.PutObjectAsync(putRequest1);
      }
      catch (Exception ex)
      {
        _logger.LogError($"Error Writing File string Data to S3 -{ex.Message}");
      }
    }
  }
  }

