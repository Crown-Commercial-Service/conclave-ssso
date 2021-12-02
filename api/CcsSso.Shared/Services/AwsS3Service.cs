using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class AwsS3Service : IAwsS3Service
  {
    private readonly IAmazonS3 _client;
    private readonly S3ConfigurationInfo _s3ConfigurationInfo;

    public AwsS3Service(S3ConfigurationInfo s3ConfigurationInfo)
    {
      _s3ConfigurationInfo = s3ConfigurationInfo;
      AmazonS3Config amazonS3Config = new AmazonS3Config
      {
        ServiceURL = _s3ConfigurationInfo.ServiceUrl
      };

      _client = new AmazonS3Client(_s3ConfigurationInfo.AccessKeyId, _s3ConfigurationInfo.AccessSecretKey, amazonS3Config);
    }

    /// <summary>
    /// Write a file
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="contentType"></param>
    /// <param name="bucketName"></param>
    /// <param name="fileContent"></param>
    /// <returns></returns>
    public async Task WritingAnObjectAsync(string fileKey, string contentType, string bucketName, Stream fileContent)
    {
      var putRequest1 = new PutObjectRequest
      {
        BucketName = bucketName,
        Key = fileKey,
        InputStream = fileContent,
        ContentType = contentType
      };

      PutObjectResponse response = await _client.PutObjectAsync(putRequest1);
    }

    /// <summary>
    /// Get the signed url for a file
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="bucketName"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public string GeneratePreSignedURL(string fileKey, string bucketName, int duration)
    {
      GetPreSignedUrlRequest preSignedUrlRequest = new GetPreSignedUrlRequest
      {
        BucketName = bucketName,
        Key = fileKey,
        Expires = DateTime.UtcNow.AddHours(duration)
      };
      var urlString = _client.GetPreSignedURL(preSignedUrlRequest);
      return urlString;
    }

    /// <summary>
    /// Read the file object as string
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="bucketName"></param>
    /// <returns></returns>
    public async Task<string> ReadObjectDataStringAsync(string fileKey, string bucketName)
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
  }
}
