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
    private readonly ICcsSsoEmailService _ccsSsoEmailService;
    private readonly ApplicationConfigurationInfo _appConfigInfo;

    public DataMigrationService(ApplicationConfigurationInfo appConfigInfo, S3ConfigurationInfo s3ConfigurationInfo,
      IAwsS3Service awsS3Service, IDocUploadService docUploadService, IDataContext dataContext,
      DocUploadConfig docUploadConfig, IDataMigrationFileContentService dataMigrationFileValidatorService,
      ICcsSsoEmailService ccsSsoEmailService)
    {
      _appConfigInfo = appConfigInfo;
      _s3ConfigurationInfo = s3ConfigurationInfo;
      _awsS3Service = awsS3Service;
      _docUploadService = docUploadService;
      _dataContext = dataContext;
      _docUploadConfig = docUploadConfig;
      _dataMigrationFileValidatorService = dataMigrationFileValidatorService;
      _ccsSsoEmailService = ccsSsoEmailService;
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
      ValidateFileExtension(file);

      var dataMigrationStatusResponse = new DataMigrationStatusResponse { ErrorDetails = new List<KeyValuePair<string, string>>() };
      var fileKeyId = Guid.NewGuid().ToString();
      var fileKey = GetUploadFileKey(fileKeyId);
      var fileStream = file.OpenReadStream();
      await _awsS3Service.WritingAnObjectAsync(fileKey, _docUploadConfig.DefaultTypeValidationValue, _s3ConfigurationInfo.DataMigrationBucketName, fileStream);
      var signedFileUrl = _awsS3Service.GeneratePreSignedURL(fileKey, _s3ConfigurationInfo.DataMigrationBucketName, _s3ConfigurationInfo.FileAccessExpirationInHours);
      var docUploadResult = await _docUploadService.UploadFileAsync(_docUploadConfig.DefaultTypeValidationValue, _docUploadConfig.DefaultSizeValidationValue, null, signedFileUrl);

      DataMigrationDetail dataMigrationDetail = new DataMigrationDetail
      {
        FileName = file.FileName,
        FileKeyId = fileKeyId,
        FileKey = fileKey,
        DocUploadId = docUploadResult.Id,
        DataMigrationStatus = DataMigrationStatus.Uploading,
        ValidationErrorDetails = JsonConvert.SerializeObject(new List<KeyValuePair<string, string>>())
      };

      _dataContext.DataMigrationDetail.Add(dataMigrationDetail);
      await _dataContext.SaveChangesAsync();
      dataMigrationStatusResponse.Id = fileKeyId;
      dataMigrationStatusResponse.DataMigrationStatus = DataMigrationStatus.Uploading;
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

      await GetDataMigrationStatusAsync(dataMigrationStatusResponse, dataMigrationDetail);

      return dataMigrationStatusResponse;
    }


    public async Task<DataMigrationListResponse> GetAllAsync(ResultSetCriteria resultSetCriteria)
    {
      var dataMigration = await _dataContext.GetPagedResultAsync(_dataContext.DataMigrationDetail
        .Where(x => !x.IsDeleted)
        .Select(dataMigrationDetail => new DataMigrationListInfo
        {
          Id = dataMigrationDetail.FileKeyId,
          FileName = dataMigrationDetail.FileName,
          DateOfUpload = dataMigrationDetail.CreatedOnUtc,
          Status = dataMigrationDetail.DataMigrationStatus,
          CreatedUserId = dataMigrationDetail.CreatedUserId,
        })
        .OrderByDescending(o => o.Id), resultSetCriteria);

      await PopulateNameOfUser(dataMigration);

      var dataMigrationListResponse = new DataMigrationListResponse
      {
        CurrentPage = dataMigration.CurrentPage,
        PageCount = dataMigration.PageCount,
        RowCount = dataMigration.RowCount,
        DataMigrationList = dataMigration.Results ?? new List<DataMigrationListInfo>()
      };

      return dataMigrationListResponse;
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
      if (docUploadDetails.State == "processing")
      {
        dataMigrationStatus = DataMigrationStatus.Uploading;
      }
      else if (docUploadDetails.State == "safe")
      {
        dataMigrationStatus = await SetValidationSafeStatusAsync(fileKey, dataMigrationDetail, errorDetails);
      }
      else // Not safe may be virus in file
      {
        errorDetails.Add(new KeyValuePair<string, string>("File validation failed", "Unsafe file"));
        dataMigrationStatus = await SetValidationFailedStatusAsync(dataMigrationDetail, errorDetails);
      }
      return (dataMigrationStatus: dataMigrationStatus, errorDetails: errorDetails);
    }

    /// <summary>
    /// To set validation safe
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="dataMigrationDetail"></param>
    /// <param name="errorDetails"></param>
    /// <returns></returns>
    private async Task<DataMigrationStatus> SetValidationSafeStatusAsync(string fileKey, DataMigrationDetail dataMigrationDetail, List<KeyValuePair<string, string>> errorDetails)
    {
      await SaveValidationStatusAsync(DataMigrationStatus.Validating, dataMigrationDetail, errorDetails);
      var errors = await ValidateUploadedFileAsync(fileKey);
      if (!errors.Any())
      {
        return await SetValidationProcessingStatusAsync(dataMigrationDetail, errorDetails);
      }
      else
      {
        errorDetails.AddRange(errors);
        return await SetValidationFailedStatusAsync(dataMigrationDetail, errorDetails);
      }
    }

    /// <summary>
    /// To set validation processing
    /// </summary>
    /// <param name="dataMigrationDetail"></param>
    /// <param name="errorDetails"></param>
    /// <returns></returns>
    private async Task<DataMigrationStatus> SetValidationProcessingStatusAsync(DataMigrationDetail dataMigrationDetail, List<KeyValuePair<string, string>> errorDetails)
    {
      await SaveValidationStatusAsync(DataMigrationStatus.Processing, dataMigrationDetail, errorDetails, DateTime.UtcNow);
      return DataMigrationStatus.Processing;
    }

    /// <summary>
    /// To set validation failed
    /// </summary>
    /// <param name="dataMigrationDetail"></param>
    /// <param name="errorDetails"></param>
    /// <returns></returns>
    private async Task<DataMigrationStatus> SetValidationFailedStatusAsync(DataMigrationDetail dataMigrationDetail, List<KeyValuePair<string, string>> errorDetails)
    {
      await SaveValidationStatusAsync(DataMigrationStatus.Failed, dataMigrationDetail, errorDetails);
      await SendDataMigrationValidationFailedAsync(dataMigrationDetail);
      return DataMigrationStatus.Failed;
    }

    private async Task SendDataMigrationValidationFailedAsync(DataMigrationDetail dataMigrationDetail)
    {
      try
      {
        var user = await _dataContext.User.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == dataMigrationDetail.CreatedUserId);
        var errorPagelink = $"{_appConfigInfo.DataMigrationSettings.DataMigrationErrorPageUrl}/{dataMigrationDetail.FileKeyId}";
        if (user != null)
        {
          await _ccsSsoEmailService.SendDataMigrationValidationFailedAsync(user.UserName, errorPagelink);
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error sending email for data migration faild notification id: {dataMigrationDetail.FileKeyId}, error: {ex.Message}");
        Console.Error.WriteLine(JsonConvert.SerializeObject(ex));
      }
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
      var errorDetails = await _dataMigrationFileValidatorService.ValidateUploadedFile(fileKey, fileContentString);
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

    /// <summary>
    /// To validate file extension
    /// </summary>
    /// <param name="file"></param>
    /// <exception cref="CcsSsoException"></exception>
    private static void ValidateFileExtension(IFormFile file)
    {
      var extension = Path.GetExtension(file.FileName);
      if (extension.ToLower() != ".csv")
      {
        throw new CcsSsoException("INVALID_DATA_MIGRATION_FILE_TYPE");
      }
    }

    /// <summary>
    /// To get data migration report details
    /// </summary>
    /// <param name="dataMigrationDetail"></param>
    /// <returns></returns>
    private DataMigrationMigrationReportDetails GetDataMigrationReportDetails(DataMigrationDetail dataMigrationDetail)
    {
      return new()
      {
        MigrationStartedTime = dataMigrationDetail.MigrationStartedOnUtc,
        MigrationEndTime = dataMigrationDetail.MigrationEndedOnUtc,
        DataMigrationFileContentRowList = _dataMigrationFileValidatorService.GetFileContentObject(dataMigrationDetail.MigrationStringContent),
      };
    }

    /// <summary>
    /// To populate name of user who uploaded file for data migration
    /// </summary>
    /// <param name="dataMigration"></param>
    /// <returns></returns>
    private async Task PopulateNameOfUser(PagedResultSet<DataMigrationListInfo> dataMigration)
    {
      if (dataMigration.Results != null && dataMigration.Results.Count > 0)
      {
        var createdUserIds = dataMigration.Results.Select(x => x.CreatedUserId).ToList();

        var users = await _dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person)
          .Where(u => !u.IsDeleted && createdUserIds.Contains(u.Id))
          .ToListAsync();

        foreach (var dataMigrationResult in dataMigration.Results)
        {
          var user = users.FirstOrDefault(x => x.Id == dataMigrationResult.CreatedUserId);
          var fName = user?.Party.Person.FirstName;
          var lName = user?.Party.Person.LastName;

          dataMigrationResult.Name = String.Join(fName, " ", lName);
        }
      }
    }

    /// <summary>
    /// To get data migration status
    /// </summary>
    /// <param name="dataMigrationStatusResponse"></param>
    /// <param name="dataMigrationDetail"></param>
    /// <returns></returns>
    private async Task GetDataMigrationStatusAsync(DataMigrationStatusResponse dataMigrationStatusResponse, DataMigrationDetail dataMigrationDetail)
    {
      if (dataMigrationDetail.DataMigrationStatus == DataMigrationStatus.Uploading)
      {
        var validationStatus = await GetValidationProcessingStatusAsync(dataMigrationDetail.FileKey, dataMigrationDetail);
        dataMigrationStatusResponse.DataMigrationStatus = validationStatus.dataMigrationStatus;
        dataMigrationStatusResponse.ErrorDetails = validationStatus.errorDetails;
      }
      else if (dataMigrationDetail.DataMigrationStatus == DataMigrationStatus.Completed)
      {
        DataMigrationMigrationReportDetails dataMigrationMigrationReportDetails = GetDataMigrationReportDetails(dataMigrationDetail);
        dataMigrationStatusResponse.DataMigrationStatus = dataMigrationDetail.DataMigrationStatus;
        dataMigrationStatusResponse.DataMigrationMigrationReportDetails = dataMigrationMigrationReportDetails;
      }
      else
      {
        dataMigrationStatusResponse.DataMigrationStatus = dataMigrationDetail.DataMigrationStatus;
        dataMigrationStatusResponse.ErrorDetails = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(dataMigrationDetail.ValidationErrorDetails);
      }
    }
  }
}
