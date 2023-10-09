using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DataMigrationJobScheduler.Contracts;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S3ConfigurationInfo = CcsSso.Core.DataMigrationJobScheduler.Model.S3ConfigurationInfo;
using IAwsS3Service = CcsSso.Core.DataMigrationJobScheduler.Contracts.IAwsS3Service;
using CcsSso.Domain.Contracts;
using IWrapperOrganisationService = CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts.IWrapperOrganisationService;
using System.Net;
using CcsSso.Domain.Exceptions;

namespace CcsSso.Core.DataMigrationJobScheduler.Services
{
  public class FileUploadJobService : IFileUploadJobService
  {
    private readonly ILogger<IFileUploadJobService> _logger;
    private readonly DataMigrationAppSettings _appSettings;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAwsS3Service _awsS3Service;

    public FileUploadJobService(IServiceScopeFactory factory,
      DataMigrationAppSettings appSettings,
      ILogger<FileUploadJobService> logger,
       IWrapperOrganisationService wrapperOrganisationService, Model.S3ConfigurationInfo s3ConfigurationInfo, IHttpClientFactory httpClientFactory,IAwsS3Service awsS3Service)
    {
      _appSettings = appSettings;
      _wrapperOrganisationService = wrapperOrganisationService;
      _logger = logger;
      _s3ConfigurationInfo = s3ConfigurationInfo;
      _httpClientFactory = httpClientFactory;
      _awsS3Service = awsS3Service;
    }

    public async Task PerformFileUploadJobAsync()
    {
      try
      {
        var files = await _wrapperOrganisationService.GetDataMigrationFilesList();

        if (files.DataMigrationList.Any())
        {
          _logger.LogInformation($"****** No of files found: {files.DataMigrationList.Count()}");

          foreach (var file in files.DataMigrationList)
          {
            _logger.LogInformation($"****** Reading File from S3: {file.FileKey} ***********");
            var fileContentString = await _awsS3Service.ReadObjectDataStringAsync(file.FileKey, _s3ConfigurationInfo.DataMigrationBucketName);
            _logger.LogInformation($"****** Downloaded File from S3 : {file.FileKey} ***********");

            var client = _httpClientFactory.CreateClient("DataMigrationApi");
            using (var formData = new MultipartFormDataContent())
            {
              _logger.LogInformation($"****** Converting File string Data to Memory Stream : {file.FileKey} ***********");
              using (var s3DataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContentString)))
              {
                // Create a StreamContent from the S3 data stream
                var streamContent = new StreamContent(s3DataStream);

                // Add the StreamContent to the form data
                formData.Add(streamContent, "file", "data.csv"); // You can specify the file name as needed

                _logger.LogInformation($"****** Uploading File to Data Migration API : {file.FileKey} ***********");
                // Make a POST request to the CII API
                var response = await client.PostAsync($"data-migration/migrate/format/csv", formData);
                if (response.IsSuccessStatusCode)
                {
                  _logger.LogInformation($"****** File Uploaded Successfully to Data Migration API : {file.FileKey} ***********");
                  var fileKey = GetUploadFileKey(file.FileKey, true);
                  _logger.LogInformation($"****** Moving file to Success Folder : {fileKey} ***********");
                  await _awsS3Service.WritingAFileDataAsync(fileKey, "text/csv", _s3ConfigurationInfo.DataMigrationBucketName, fileContentString);
                  _logger.LogInformation($"****** Updating File Status as Completed : {file.Id} ***********");
                  await _wrapperOrganisationService.UpdateDataMigrationFileStatus(new DataMigrationStatusRequest { Id = file.Id, DataMigrationStatus = DataMigrationStatus.Completed });
                }
                else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                  throw new CcsSsoException("CII_SERVICE_IS_TEMPORARILY_UNAVAILABLE");
                }
                else
                {
                  _logger.LogInformation($"****** File Upload to Data Migration API failed : {file.FileKey} ***********");
                  var fileKey = GetUploadFileKey(file.FileKey, false);
                  _logger.LogInformation($"****** Moving file to Failed Folder : {fileKey} ***********");
                  await _awsS3Service.WritingAFileDataAsync(fileKey, "text/csv", _s3ConfigurationInfo.DataMigrationBucketName, fileContentString);
                  _logger.LogInformation($"****** Updating File Status as Failed : {file.Id} ***********");
                  await _wrapperOrganisationService.UpdateDataMigrationFileStatus(new DataMigrationStatusRequest { Id = file.Id, DataMigrationStatus = DataMigrationStatus.Failed });
                }
              }
            }
          }
        }
        else
        {
          _logger.LogInformation($"****** No files found ******");
        }
      }
      catch(Exception ex)
      {
        _logger.LogError($"Error While Running a Job. Error-{ex.Message}");
      }
    }

    /// <summary>
    /// Get the full file key in "folderName/organisationId/fileKeyId" format.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="fileKeyId"></param>
    /// <returns></returns>
    private string GetUploadFileKey(string fileKeyId,bool isSuccess)
    {
      _logger.LogInformation($"****** Getting the full file Key : {fileKeyId} ***********");
      string fileKeyinfo= fileKeyId.Substring(fileKeyId.IndexOf('/') + 1);
      if (isSuccess)
      {
        return $"{_s3ConfigurationInfo.DataMigrationSuccessFolderName}/{fileKeyinfo}";
      }
      else
      {
        return $"{_s3ConfigurationInfo.DataMigrationFailedFolderName}/{fileKeyinfo}";
      }
    }

  }
}
