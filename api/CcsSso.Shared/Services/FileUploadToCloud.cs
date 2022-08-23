using Azure.Storage.Blobs;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Dto;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class FileUploadToCloud : IFileUploadToCloud
  {
    private readonly AzureBlobConfiguration azureBlobConfiguration;
    BlobContainerClient containerClient;

    public FileUploadToCloud(AzureBlobConfiguration _azureBlobConfiguration)
    {
      azureBlobConfiguration = _azureBlobConfiguration;


      string endpointProtocol = azureBlobConfiguration.EndpointProtocol;
      string accountName = azureBlobConfiguration.AccountName;
      string accountKey = azureBlobConfiguration.AccountKey;
      string endpointAzure = azureBlobConfiguration.EndpointAzure;

      string azureBlobContainer = azureBlobConfiguration.AzureBlobContainer;

      BlobServiceClient blobServiceClient = new BlobServiceClient(endpointProtocol + accountName + accountKey + endpointAzure);

      containerClient = blobServiceClient.GetBlobContainerClient(azureBlobContainer);

    }

    public async Task<AzureResponse> FileUploadToAzureBlobAsync(byte[] stream, string inputFileType)
    {
      var csvDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss");
      string fileExtension = azureBlobConfiguration.FileExtension;
      string filePathPrefix = azureBlobConfiguration.FilePathPrefix;
      string blobFileName = $"{filePathPrefix}{csvDate}_{inputFileType}{fileExtension}"; 

      Console.WriteLine($"FileUploadToAzureBlobAsync > fileName= {blobFileName }");


      AzureResponse azureResponse = new AzureResponse();

      try
      {
        BlobClient blobClient = containerClient.GetBlobClient(blobFileName); 
                                
        MemoryStream blobStream = new MemoryStream(stream);
        blobStream.Position = 0;

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
        Console.WriteLine($"FileUploadToAzureBlobAsync > Exception file type= {e.Message}");

        azureResponse.responseMessage = e.Message + "BlobAlreadyExists";
        azureResponse.responseStatus = false;
        azureResponse.responseFileName = "fileName"; 
      }
      return azureResponse;
    }

  }
}
