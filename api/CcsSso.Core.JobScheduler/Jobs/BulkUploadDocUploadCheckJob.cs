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
  public class BulkUploadDocUploadCheckJob : BackgroundService
  {
    private readonly AppSettings _appSettings;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailSupportService _emailSupportService;
    private IDataContext _dataContext;
    private IDocUploadService _docUploadService;
    
    public BulkUploadDocUploadCheckJob(AppSettings appSettings, IServiceProvider serviceProvider,
      IEmailSupportService emailSupportService)
    {
      _appSettings = appSettings;
      _serviceProvider = serviceProvider;
      _emailSupportService = emailSupportService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      //using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
      //{
      //  while (!stoppingToken.IsCancellationRequested)
      //  {
      //    _dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
      //    _docUploadService = scope.ServiceProvider.GetRequiredService<IDocUploadService>();
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
        b.BulkUploadStatus == BulkUploadStatus.Processing).ToListAsync();

      await VerifyFromDocUploadAsync(bulkUploads);

      await _dataContext.SaveChangesAsync();

    }

    private async Task VerifyFromDocUploadAsync(List<BulkUploadDetail> bulkUploadDetailsList)
    {
      foreach (var bulkUploadDetail in bulkUploadDetailsList)
      {
        var errorDetails = new List<KeyValuePair<string, string>>();
        var docUploadDetails = await _docUploadService.GetFileStatusAsync(bulkUploadDetail.DocUploadId);
        if (docUploadDetails.State == "processing")
        {
          continue;
        }
        else if (docUploadDetails.State == "safe")
        {
          bulkUploadDetail.BulkUploadStatus = BulkUploadStatus.Validating;
          bulkUploadDetail.ValidationErrorDetails = JsonConvert.SerializeObject(errorDetails);          
        }
        else // Not safe may be virus in file
        {
          errorDetails.Add(new KeyValuePair<string, string>("File validation failed", "Unsafe file"));
          bulkUploadDetail.BulkUploadStatus = BulkUploadStatus.DocUploadValidationFail;
          bulkUploadDetail.ValidationErrorDetails = JsonConvert.SerializeObject(errorDetails);
          // TODO Delete the file
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
              Console.Error.WriteLine($"Error Sending email for Bulk Upload DocUpload id: {bulkUploadDetail.FileKeyId}, error: {ex.Message}");
              Console.Error.WriteLine(JsonConvert.SerializeObject(ex));
            }
          }
        }
      }
    }

  }
}
