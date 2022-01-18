using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
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

namespace CcsSso.Core.Service
{
  public class BulkUploadService : IBulkUploadService
  {

    private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly IAwsS3Service _awsS3Service;
    private readonly IDocUploadService _docUploadService;
    private readonly IDataContext _dataContext;
    private readonly DocUploadConfig _docUploadConfig;
    private readonly IBulkUploadFileContentService _bulkUploadFileValidatorService;
    public BulkUploadService(S3ConfigurationInfo s3ConfigurationInfo, IAwsS3Service awsS3Service, IDocUploadService docUploadService,
      IDataContext dataContext, DocUploadConfig docUploadConfig, IBulkUploadFileContentService bulkUploadFileValidatorService)
    {
      _s3ConfigurationInfo = s3ConfigurationInfo;
      _awsS3Service = awsS3Service;
      _docUploadService = docUploadService;
      _dataContext = dataContext;
      _docUploadConfig = docUploadConfig;
      _bulkUploadFileValidatorService = bulkUploadFileValidatorService;
    }

    /// <summary>
    /// This method upload the file to S3 bucket and provide that url to DocUpload service and return accepted status.
    /// File will be uploaded to a folder in he bucket. File key (name) is in "folderName/organisationId/fileKeyId" format.
    /// FileKeyId is just a unique id (GUID) whihc will be used to query the bulk upload process status.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<BulkUploadStatusResponse> BulkUploadUsersAsync(string organisationId, IFormFile file)
    {
      var extension = Path.GetExtension(file.FileName);
      if (extension.ToLower() != ".csv")
      {
        throw new CcsSsoException("INVALID_BULKUPLOAD_FILE_TYPE");
      }
      var bulkUploadStatusResponse = new BulkUploadStatusResponse { ErrorDetails = new List<KeyValuePair<string, string>>() };
      var fileKeyId = Guid.NewGuid().ToString();
      var fileKey = GetUploadFileKey(organisationId, fileKeyId);
      var fileStream = file.OpenReadStream();
      await _awsS3Service.WritingAnObjectAsync(fileKey, _docUploadConfig.DefaultTypeValidationValue, _s3ConfigurationInfo.BulkUploadBucketName, fileStream);
      var signedFileUrl = _awsS3Service.GeneratePreSignedURL(fileKey, _s3ConfigurationInfo.BulkUploadBucketName, _s3ConfigurationInfo.FileAccessExpirationInHours);
      var docUploadResult = await _docUploadService.UploadFileAsync(_docUploadConfig.DefaultTypeValidationValue, _docUploadConfig.DefaultSizeValidationValue, null, signedFileUrl);

      BulkUploadDetail bulkUploadDetail = new BulkUploadDetail
      {
        OrganisationId = organisationId,
        FileKeyId = fileKeyId,
        FileKey = fileKey,
        DocUploadId = docUploadResult.Id,
        BulkUploadStatus = BulkUploadStatus.Processing,
        ValidationErrorDetails = JsonConvert.SerializeObject(new List<KeyValuePair<string, string>>())
      };

      _dataContext.BulkUploadDetail.Add(bulkUploadDetail);
      await _dataContext.SaveChangesAsync();
      bulkUploadStatusResponse.Id = fileKeyId;
      bulkUploadStatusResponse.BulkUploadStatus = BulkUploadStatus.Processing;
      return bulkUploadStatusResponse;
    }

    ///// <summary>
    ///// This method checks the bulk uplod status.
    ///// </summary>
    ///// <param name="organisationId"></param>
    ///// <param name="fileKeyId"></param>
    ///// <returns></returns>
    //public async Task<BulkUploadStatusResponse> CheckBulkUploadStatusAsync(string organisationId, string fileKeyId)
    //{
    //  var bulkUploadStatusResponse = new BulkUploadStatusResponse { Id = fileKeyId, ErrorDetails = new List<KeyValuePair<string, string>>() };

    //  var bulkUploadDetail = await _dataContext.BulkUploadDetail.FirstOrDefaultAsync(b => !b.IsDeleted && b.FileKeyId == fileKeyId && b.OrganisationId == organisationId);

    //  if (bulkUploadDetail == null)
    //  {
    //    throw new ResourceNotFoundException();
    //  }

    //  bulkUploadStatusResponse.BulkUploadStatus = bulkUploadDetail.BulkUploadStatus;
    //  bulkUploadStatusResponse.ErrorDetails = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(bulkUploadDetail.ValidationErrorDetails);

    //  if (bulkUploadDetail.BulkUploadStatus == BulkUploadStatus.MigrationCompleted)
    //  {
    //    BulkUploadMigrationReportDetails bulkUploadMigrationReportDetails = new ()
    //    {
    //      TotalOrganisationCount= bulkUploadDetail.TotalOrganisationCount,
    //      TotalUserCount= bulkUploadDetail.TotalUserCount,
    //      ProcessedUserCount = bulkUploadDetail.ProcessedUserCount,
    //      FailedUserCount = bulkUploadDetail.FailedUserCount,
    //      MigrationStartedTime = bulkUploadDetail.MigrationStartedOnUtc,
    //      MigrationEndTime = bulkUploadDetail.MigrationEndedOnUtc,
    //      // TODO send the content
    //    };
    //  }

    //  return bulkUploadStatusResponse;
    //}


    /// <summary>
    /// This method checks the bulk uplod status. And used to poll from the UI untill validation is completed.
    /// This check the status in the DB and if processing check the actual file status if not send the validation information available in the db.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="fileKeyId"></param>
    /// <returns></returns>
    public async Task<BulkUploadStatusResponse> CheckBulkUploadStatusAsync(string organisationId, string fileKeyId)
    {
      var bulkUploadStatusResponse = new BulkUploadStatusResponse { Id = fileKeyId, ErrorDetails = new List<KeyValuePair<string, string>>() };

      var bulkUploadDetail = await _dataContext.BulkUploadDetail.FirstOrDefaultAsync(b => !b.IsDeleted && b.FileKeyId == fileKeyId && b.OrganisationId == organisationId);

      if (bulkUploadDetail == null)
      {
        throw new ResourceNotFoundException();
      }

      if (bulkUploadDetail.BulkUploadStatus == BulkUploadStatus.Processing)
      {
        var validationStatus = await GetValidationProcessingStatusAsync(bulkUploadDetail.FileKey, bulkUploadDetail);
        bulkUploadStatusResponse.BulkUploadStatus = validationStatus.bulkUploadStatus;
        bulkUploadStatusResponse.ErrorDetails = validationStatus.errorDetails;
      }
      else if (bulkUploadDetail.BulkUploadStatus == BulkUploadStatus.MigrationCompleted)
      {
        BulkUploadMigrationReportDetails bulkUploadMigrationReportDetails = new()
        {
          TotalOrganisationCount = bulkUploadDetail.TotalOrganisationCount,
          TotalUserCount = bulkUploadDetail.TotalUserCount,
          ProcessedUserCount = bulkUploadDetail.ProcessedUserCount,
          FailedUserCount = bulkUploadDetail.FailedUserCount,
          MigrationStartedTime = bulkUploadDetail.MigrationStartedOnUtc,
          MigrationEndTime = bulkUploadDetail.MigrationEndedOnUtc,
          BulkUploadFileContentRowList = _bulkUploadFileValidatorService.GetFileContentObject(bulkUploadDetail.MigrationStringContent),
        };
        bulkUploadStatusResponse.BulkUploadStatus = bulkUploadDetail.BulkUploadStatus;
        bulkUploadStatusResponse.BulkUploadMigrationReportDetails = bulkUploadMigrationReportDetails;
      }
      else
      {
        bulkUploadStatusResponse.BulkUploadStatus = bulkUploadDetail.BulkUploadStatus;
        bulkUploadStatusResponse.ErrorDetails = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(bulkUploadDetail.ValidationErrorDetails);
      }
      return bulkUploadStatusResponse;
    }

    /// <summary>
    /// Check the actual status of the file processing
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="bulkUploadDetail"></param>
    /// <returns></returns>
    private async Task<(BulkUploadStatus bulkUploadStatus, List<KeyValuePair<string, string>> errorDetails)> GetValidationProcessingStatusAsync(string fileKey, BulkUploadDetail bulkUploadDetail)
    {
      BulkUploadStatus bulkUploadStatus;
      var errorDetails = new List<KeyValuePair<string, string>>();
      var docUploadDetails = await _docUploadService.GetFileStatusAsync(bulkUploadDetail.DocUploadId);
      //errorDetails.Add(new KeyValuePair<string, string>("File validation failed", "Unsafe file"));
      //errorDetails.Add(new KeyValuePair<string, string>("Invalid Value", "Email in row 1"));
      if (docUploadDetails.State == "processing")
      {
        bulkUploadStatus = BulkUploadStatus.Processing;
      }
      else if (docUploadDetails.State == "safe")
      {
        await SaveValidationStatusAsync(BulkUploadStatus.Validating, bulkUploadDetail, errorDetails);

        // At the moment validation is done in the same time (without handing overto a background job). Beacuse of that there will be a max file size.
        var errors = await ValidateUploadedFileAsync(fileKey);
        if (!errors.Any()) // No errors
        {
          // TODO Push to DM
          await SaveValidationStatusAsync(BulkUploadStatus.Migrating, bulkUploadDetail, errorDetails, DateTime.UtcNow);
          bulkUploadStatus = BulkUploadStatus.Migrating;
        }
        else
        {
          errorDetails.AddRange(errors);
          await SaveValidationStatusAsync(BulkUploadStatus.ValidationFail, bulkUploadDetail, errorDetails);
          bulkUploadStatus = BulkUploadStatus.ValidationFail;
        }
      }
      else // Not safe may be virus in file
      {
        errorDetails.Add(new KeyValuePair<string, string>("File validation failed", "Unsafe file"));
        await SaveValidationStatusAsync(BulkUploadStatus.DocUploadValidationFail, bulkUploadDetail, errorDetails);
        // TODO Delete the file
        bulkUploadStatus = BulkUploadStatus.DocUploadValidationFail;
      }
      return (bulkUploadStatus: bulkUploadStatus, errorDetails: errorDetails);
    }

    /// <summary>
    /// During the status check update the db with inetermediate status
    /// </summary>
    /// <param name="status"></param>
    /// <param name="bulkUploadDetail"></param>
    /// <param name="errorDetails"></param>
    /// <returns></returns>
    private async Task SaveValidationStatusAsync(BulkUploadStatus status, BulkUploadDetail bulkUploadDetail, List<KeyValuePair<string, string>> errorDetails, DateTime? migrationStartedTime = null)
    {
      bulkUploadDetail.BulkUploadStatus = status;
      bulkUploadDetail.ValidationErrorDetails = JsonConvert.SerializeObject(errorDetails);
      if (migrationStartedTime != null)
      {
        bulkUploadDetail.MigrationStartedOnUtc = migrationStartedTime.Value;
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
      var fileContentString = await _awsS3Service.ReadObjectDataStringAsync(fileKey, _s3ConfigurationInfo.BulkUploadBucketName);
      var errorDetails = _bulkUploadFileValidatorService.ValidateUploadedFile(fileKey, fileContentString);
      return errorDetails;
    }

    /// <summary>
    /// Get the full file key in "folderName/organisationId/fileKeyId" format.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="fileKeyId"></param>
    /// <returns></returns>
    private string GetUploadFileKey(string organisationId, string fileKeyId)
    {
      return $"{_s3ConfigurationInfo.BulkUploadFolderName}/{organisationId}/{fileKeyId}.{_docUploadConfig.DefaultTypeValidationValue}";
    }

  }
}
