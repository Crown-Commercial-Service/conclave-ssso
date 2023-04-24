using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Service.External
{
  public class DataMigrationService : IDataMigrationService
  {

    private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly IAwsS3Service _awsS3Service;
    private readonly IDocUploadService _docUploadService;
    private readonly IDataContext _dataContext;
    private readonly DocUploadConfig _docUploadConfig;
    private readonly IDataMigrationFileContentService _dataMigrationFileValidatorService;
    public DataMigrationService(S3ConfigurationInfo s3ConfigurationInfo, IAwsS3Service awsS3Service, IDocUploadService docUploadService,
      IDataContext dataContext, DocUploadConfig docUploadConfig, IDataMigrationFileContentService dataMigrationFileValidatorService)
    {
      _s3ConfigurationInfo = s3ConfigurationInfo;
      _awsS3Service = awsS3Service;
      _docUploadService = docUploadService;
      _dataContext = dataContext;
      _docUploadConfig = docUploadConfig;
      _dataMigrationFileValidatorService = dataMigrationFileValidatorService;
    }

    /// <summary>
    /// This method upload the file to S3 bucket and provide that url to DocUpload service and return accepted status.
    /// File will be uploaded to a folder in he bucket. File key (name) is in "folderName/organisationId/fileKeyId" format.
    /// FileKeyId is just a unique id (GUID) whihc will be used to query the data migration process status.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<DataMigrationStatusResponse> UploadDataMigrationFileAsync(IFormFile file)
    {
      var extension = Path.GetExtension(file.FileName);
      if (extension.ToLower() != ".csv")
      {
        throw new CcsSsoException("INVALID_BULKUPLOAD_FILE_TYPE");
      }
      var dataMigrationStatusResponse = new DataMigrationStatusResponse { ErrorDetails = new List<KeyValuePair<string, string>>() };
      var fileKeyId = Guid.NewGuid().ToString();
      var fileKey = GetUploadFileKey(fileKeyId);
      var fileStream = file.OpenReadStream();
      await _awsS3Service.WritingAnObjectAsync(fileKey, _docUploadConfig.DefaultTypeValidationValue, _s3ConfigurationInfo.DataMigrationBucketName, fileStream);
      var signedFileUrl = _awsS3Service.GeneratePreSignedURL(fileKey, _s3ConfigurationInfo.DataMigrationBucketName, _s3ConfigurationInfo.FileAccessExpirationInHours);
      var docUploadResult = await _docUploadService.UploadFileAsync(_docUploadConfig.DefaultTypeValidationValue, _docUploadConfig.DefaultSizeValidationValue, null, signedFileUrl);

      DataMigrationDetail dataMigrationDetail = new DataMigrationDetail
      {
        FileKeyId = fileKeyId,
        FileKey = fileKey,
        DocUploadId = docUploadResult.Id,
        DataMigrationStatus = DataMigrationStatus.DocUploading,
        ValidationErrorDetails = JsonConvert.SerializeObject(new List<KeyValuePair<string, string>>())
      };

      _dataContext.DataMigrationDetail.Add(dataMigrationDetail);
      await _dataContext.SaveChangesAsync();
      dataMigrationStatusResponse.Id = fileKeyId;
      dataMigrationStatusResponse.DataMigrationStatus = DataMigrationStatus.DocUploading;
      return dataMigrationStatusResponse;
    }

    /// <summary>
    /// This method checks the data migration status. And used to poll from the UI untill validation is completed.
    /// This check the status in the DB and if processing check the actual file status if not send the validation information available in the db.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="fileKeyId"></param>
    /// <returns></returns>
    public async Task<DataMigrationStatusResponse> CheckDataMigrationStatusAsync(string fileKeyId)
    {
      var dataMigrationStatusResponse = new DataMigrationStatusResponse { Id = fileKeyId, ErrorDetails = new List<KeyValuePair<string, string>>() };

      var dataMigrationDetail = await _dataContext.DataMigrationDetail.FirstOrDefaultAsync(b => !b.IsDeleted && b.FileKeyId == fileKeyId);

      if (dataMigrationDetail == null)
      {
        throw new ResourceNotFoundException();
      }

      if (dataMigrationDetail.DataMigrationStatus == DataMigrationStatus.DocUploading)
      {
        var validationStatus = await GetValidationProcessingStatusAsync(dataMigrationDetail.FileKey, dataMigrationDetail);
        dataMigrationStatusResponse.DataMigrationStatus = validationStatus.dataMigrationStatus;
        dataMigrationStatusResponse.ErrorDetails = validationStatus.errorDetails;
      }
      else if (dataMigrationDetail.DataMigrationStatus == DataMigrationStatus.MigrationCompleted)
      {
        DataMigrationMigrationReportDetails dataMigrationMigrationReportDetails = new()
        {
          //TotalOrganisationCount = dataMigrationDetail.TotalOrganisationCount,
          //TotalUserCount = dataMigrationDetail.TotalUserCount,
          //ProcessedUserCount = dataMigrationDetail.ProcessedUserCount,
          //FailedUserCount = dataMigrationDetail.FailedUserCount,
          MigrationStartedTime = dataMigrationDetail.MigrationStartedOnUtc,
          MigrationEndTime = dataMigrationDetail.MigrationEndedOnUtc,
          DataMigrationFileContentRowList = _dataMigrationFileValidatorService.GetFileContentObject(dataMigrationDetail.MigrationStringContent),
        };
        dataMigrationStatusResponse.DataMigrationStatus = dataMigrationDetail.DataMigrationStatus;
        dataMigrationStatusResponse.DataMigrationMigrationReportDetails = dataMigrationMigrationReportDetails;
      }
      else
      {
        dataMigrationStatusResponse.DataMigrationStatus = dataMigrationDetail.DataMigrationStatus;
        dataMigrationStatusResponse.ErrorDetails = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(dataMigrationDetail.ValidationErrorDetails);
      }
      return dataMigrationStatusResponse;
    }

    /// <summary>
    /// Check the actual status of the file processing
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="dataMigrationDetail"></param>
    /// <returns></returns>
    private async Task<(DataMigrationStatus dataMigrationStatus, List<KeyValuePair<string, string>> errorDetails)> GetValidationProcessingStatusAsync(string fileKey, DataMigrationDetail dataMigrationDetail)
    {
      DataMigrationStatus dataMigrationStatus;
      var errorDetails = new List<KeyValuePair<string, string>>();
      var docUploadDetails = await _docUploadService.GetFileStatusAsync(dataMigrationDetail.DocUploadId);
      //errorDetails.Add(new KeyValuePair<string, string>("File validation failed", "Unsafe file"));
      //errorDetails.Add(new KeyValuePair<string, string>("Invalid Value", "Email in row 1"));
      if (docUploadDetails.State == "processing")
      {
        dataMigrationStatus = DataMigrationStatus.DocUploading;
      }
      else if (docUploadDetails.State == "safe")
      {
        await SaveValidationStatusAsync(DataMigrationStatus.Validating, dataMigrationDetail, errorDetails);

        // At the moment validation is done in the same time (without handing overto a background job). Beacuse of that there will be a max file size.
        var errors = await ValidateUploadedFileAsync(fileKey);
        if (!errors.Any()) // No errors
        {
          // TODO Push to DM
          await SaveValidationStatusAsync(DataMigrationStatus.Migrating, dataMigrationDetail, errorDetails, DateTime.UtcNow);
          dataMigrationStatus = DataMigrationStatus.Migrating;
        }
        else
        {
          errorDetails.AddRange(errors);
          await SaveValidationStatusAsync(DataMigrationStatus.ValidationFail, dataMigrationDetail, errorDetails);
          dataMigrationStatus = DataMigrationStatus.ValidationFail;
        }
      }
      else // Not safe may be virus in file
      {
        errorDetails.Add(new KeyValuePair<string, string>("File validation failed", "Unsafe file"));
        await SaveValidationStatusAsync(DataMigrationStatus.DocUploadValidationFail, dataMigrationDetail, errorDetails);
        // TODO Delete the file
        dataMigrationStatus = DataMigrationStatus.DocUploadValidationFail;
      }
      return (dataMigrationStatus: dataMigrationStatus, errorDetails: errorDetails);
    }

    /// <summary>
    /// During the status check update the db with inetermediate status
    /// </summary>
    /// <param name="status"></param>
    /// <param name="dataMigrationDetail"></param>
    /// <param name="errorDetails"></param>
    /// <returns></returns>
    private async Task SaveValidationStatusAsync(DataMigrationStatus status, DataMigrationDetail dataMigrationDetail, List<KeyValuePair<string, string>> errorDetails, DateTime? migrationStartedTime = null)
    {
      dataMigrationDetail.DataMigrationStatus = status;
      dataMigrationDetail.ValidationErrorDetails = JsonConvert.SerializeObject(errorDetails);
      if (migrationStartedTime != null)
      {
        dataMigrationDetail.MigrationStartedOnUtc = migrationStartedTime.Value;
      }
      await _dataContext.SaveChangesAsync();
    }

    /// <summary>
    /// Validate the file locally
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    private async Task<List<KeyValuePair<string, string>>> ValidateUploadedFileAsync(string fileKey)
    {
      var fileContentString = await _awsS3Service.ReadObjectDataStringAsync(fileKey, _s3ConfigurationInfo.DataMigrationBucketName);
      var errorDetails = _dataMigrationFileValidatorService.ValidateUploadedFile(fileKey, fileContentString);
      return errorDetails;
    }

    /// <summary>
    /// Get the full file key in "folderName/organisationId/fileKeyId" format.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="fileKeyId"></param>
    /// <returns></returns>
    private string GetUploadFileKey(string fileKeyId)
    {
      return $"{_s3ConfigurationInfo.DataMigrationFolderName}/{fileKeyId}.{_docUploadConfig.DefaultTypeValidationValue}";
    }

  }
}
