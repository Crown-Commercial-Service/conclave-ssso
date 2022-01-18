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
  public class BulkUploadMigrationStatusCheckJob : BackgroundService
  {
    private readonly AppSettings _appSettings;
    private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAwsS3Service _awsS3Service;
    private readonly IEmailSupportService _emailSupportService;
    private IDataContext _dataContext;
    private IBulkUploadFileContentService _bulkUploadFileValidatorService;

    public BulkUploadMigrationStatusCheckJob(AppSettings appSettings, S3ConfigurationInfo s3ConfigurationInfo,
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
      using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
      {
        while (!stoppingToken.IsCancellationRequested)
        {
          _dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
          _bulkUploadFileValidatorService = scope.ServiceProvider.GetRequiredService<IBulkUploadFileContentService>();
          await PerformJobAsync();
          await Task.Delay(_appSettings.ScheduleJobSettings.BulkUploadJobExecutionFrequencyInMinutes * 60000, stoppingToken);
        }
      }
    }

    public void InitiateScopedServices(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task PerformJobAsync()
    {
      var bulkUploads = await _dataContext.BulkUploadDetail.Where(b => !b.IsDeleted &&
        b.BulkUploadStatus == BulkUploadStatus.Migrating).ToListAsync();

      await CheckMigratingStatusAsync(bulkUploads);

      await _dataContext.SaveChangesAsync();

    }

    private async Task CheckMigratingStatusAsync(List<BulkUploadDetail> bulkUploadDetailsList)
    {
      List<Task> emailTaskList = new();

      foreach (var bulkUploadDetail in bulkUploadDetailsList)
      {
        Console.WriteLine($"Checking migration status for bulk upload id: {bulkUploadDetail.FileKeyId}");
        var fileContentString = await _awsS3Service.ReadObjectDataStringAsync(bulkUploadDetail.FileKey, _s3ConfigurationInfo.BulkUploadBucketName);
        var migrationResult = _bulkUploadFileValidatorService.CheckMigrationStatus(fileContentString);
        if (migrationResult.IsCompleted)
        {
          bulkUploadDetail.MigrationEndedOnUtc = DateTime.UtcNow;
          bulkUploadDetail.BulkUploadStatus = BulkUploadStatus.MigrationCompleted;
          bulkUploadDetail.MigrationStringContent = fileContentString;
          bulkUploadDetail.TotalOrganisationCount = migrationResult.TotalOrganisationCount;
          bulkUploadDetail.TotalUserCount = migrationResult.TotalUserCount;
          bulkUploadDetail.ProcessedUserCount = migrationResult.ProceededUserCount;
          bulkUploadDetail.FailedUserCount = migrationResult.FailedUserCount;

          // Notify via email to the created user
          var user = await _dataContext.User.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == bulkUploadDetail.CreatedUserId);
          var reportUrl = $"{_appSettings.BulkUploadSettings.BulkUploadReportUrl}/{bulkUploadDetail.FileKeyId}";
          var bulkUploadResultString = migrationResult.FailedUserCount == 0 ?
            "Migration Completed without errors" : "Migration Completed with errors";
          if (user != null)
          {
            try
            {
              await _emailSupportService.SendBulUploadResultEmailAsync(user.UserName, bulkUploadResultString, reportUrl);
            }
            catch (Exception ex)
            {
              Console.Error.WriteLine($"Error Sending email for Bulk Upload Migration id: {bulkUploadDetail.FileKeyId}, error: {ex.Message}");
              Console.Error.WriteLine(JsonConvert.SerializeObject(ex));
            }
          }

        }
      }
    }

  }
}
