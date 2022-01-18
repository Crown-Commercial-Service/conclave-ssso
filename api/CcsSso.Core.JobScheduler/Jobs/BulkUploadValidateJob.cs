using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler
{
  public class BulkUploadValidateJob : BackgroundService
  {
    private readonly AppSettings _appSettings;
    private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAwsS3Service _awsS3Service;
    private readonly IEmailSupportService _emailSupportService;
    private IDataContext _dataContext;
    private IBulkUploadFileContentService _bulkUploadFileValidatorService;
    
    public BulkUploadValidateJob(AppSettings appSettings, S3ConfigurationInfo s3ConfigurationInfo,
      IServiceProvider serviceProvider, IAwsS3Service awsS3Service, IEmailSupportService emailSupportService)
    {
      _appSettings = appSettings;
      _s3ConfigurationInfo = s3ConfigurationInfo;
      _serviceProvider = serviceProvider;
      _awsS3Service = awsS3Service;
      _emailSupportService = emailSupportService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      //using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
      //{
      //  while (!stoppingToken.IsCancellationRequested)
      //  {
      //    _dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
      //    _bulkUploadFileValidatorService = scope.ServiceProvider.GetRequiredService<IBulkUploadFileValidatorService>();
      //    await PerformJobAsync();
      //    await Task.Delay(_appSettings.ScheduleJobSettings.BulkUploadJobExecutionFrequencyInMinutes * 60000, stoppingToken);
      //  }
      //}
    }

    public void InitiateScopedServices(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task PerformJobAsync()
    {
      var bulkUploads = await _dataContext.BulkUploadDetail.Where(b => !b.IsDeleted &&
        b.BulkUploadStatus == BulkUploadStatus.Validating).ToListAsync();

      await ValidateAndPushForMigrationAsync(bulkUploads);

      await _dataContext.SaveChangesAsync();

    }

    private async Task ValidateAndPushForMigrationAsync(List<BulkUploadDetail> bulkUploadDetailsList)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();
      foreach (var bulkUploadDetail in bulkUploadDetailsList)
      {
        var errors = await ValidateUploadedFileAsync(bulkUploadDetail.FileKey);
        if (!errors.Any()) // No errors
        {
          // TODO Push to DM
          bulkUploadDetail.MigrationStartedOnUtc = DateTime.UtcNow;
          bulkUploadDetail.BulkUploadStatus = BulkUploadStatus.Migrating;
          bulkUploadDetail.ValidationErrorDetails = JsonConvert.SerializeObject(errorDetails);
        }
        else
        {
          errorDetails.AddRange(errors);
          bulkUploadDetail.BulkUploadStatus = BulkUploadStatus.ValidationFail;
          bulkUploadDetail.ValidationErrorDetails = JsonConvert.SerializeObject(errorDetails);
          // Notify via email to the created user
          var user = await _dataContext.User.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == bulkUploadDetail.CreatedUserId);
          var reportUrl = $"{_appSettings.BulkUploadSettings.BulkUploadReportUrl}/{bulkUploadDetail.FileKeyId}";
          var bulkUploadResultString = "File validation Failed";
          if (user != null)
          {
            try
            {
              await _emailSupportService.SendBulUploadResultEmailAsync(user.UserName, bulkUploadResultString, reportUrl);
            }
            catch (Exception ex)
            {
              Console.Error.WriteLine($"Error Sending email for Bulk Upload validation id: {bulkUploadDetail.FileKeyId}, error: {ex.Message}");
              Console.Error.WriteLine(JsonConvert.SerializeObject(ex));
            }
          }
        }
      }
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

  }
}
