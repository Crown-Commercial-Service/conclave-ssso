using Azure.Storage.Blobs;
using CcsSso.Shared.Domain.Dto;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class FileUploadToCloud : IFileUploadToCloud
  {
    BlobContainerClient containerClient;

    public FileUploadToCloud()
    {
      var csvDate = DateTime.UtcNow;
      string fileheader = "PPG";
      string fileType = "Organisations"; // Input Parameter - File Type
      string fileExtension = ".csv";
      string blobFileName = csvDate + "_" + fileheader + "_" + fileType + fileExtension; // Sample File Format <date>_PPG_Organisations.csv

      string endpointProtocol = "DefaultEndpointsProtocol=https;";
      string accountName = "AccountName=publicprocurementgateway;";
      string accountKey = "AccountKey=HU36EZrIjQp+SV5e+1vc0mQltTLMTlxFyDM4t58ChbWRTyHIKQDVqz3xfCgLIawV+2rbstyGv4+uogyHNMyA7w==;";
      string endpointAzure = "EndpointSuffix=core.windows.net;";

      string azureBlobContainer = "ppg-files";
      // App Setting Values - End

        //var localFilePath = @"E:\Text.csv"; // Remove this line of code, once integrate with the ccssso application

        BlobServiceClient blobServiceClient = new BlobServiceClient(endpointProtocol + accountName + accountKey + endpointAzure);

        containerClient = blobServiceClient.GetBlobContainerClient(azureBlobContainer);

       

      }

    public async Task<AzureResponse> FileUploadToAzureBlobAsync(byte[] stream, string inputStream, string inputFileType, string location)
    {
      AzureResponse azureResponse = new AzureResponse();

      try
      {
        BlobClient blobClient = containerClient.GetBlobClient("TestOrgLocal3.csv"); 
                                
        MemoryStream blobStream = new MemoryStream(stream);

        if (!blobClient.Exists())
        {
          blobStream.Position = 0;
          await blobClient.UploadAsync(blobStream);
          azureResponse.responseMessage = "Sucess";
          azureResponse.responseStatus = true;
          azureResponse.responseFileName = blobClient.Uri.AbsoluteUri.ToString();
        }
        else
        {
          azureResponse.responseMessage = "File Already Exists ";
          azureResponse.responseStatus = false;
          azureResponse.responseFileName = blobClient.Uri.AbsoluteUri.ToString();
        }
      }
      catch (Exception e)
      {

        azureResponse.responseMessage = e.Message + "BlobAlreadyExists";
        azureResponse.responseStatus = false;
        azureResponse.responseFileName = "fileName"; 
      }
      return azureResponse;
    }


    //public async Task<AzureResponse> ReadFromAzureBlobAsync(byte[] stream, string inputStream, string inputFileType, string location, CancellationToken c)
    //{
    //  BlobClient blobClient = containerClient.GetBlobClient("TestOrgLocal3.csv");

    //  using var stream = await blobClient.OpenReadAsync(null, c);
    //    return await JsonSerializer.DeserializeAsync<T>(stream, null, c);
    //}
    //https://basantakharel.com/efficiently-transfer-c-objects-to-from-azure-blob-storage/


  }
}
