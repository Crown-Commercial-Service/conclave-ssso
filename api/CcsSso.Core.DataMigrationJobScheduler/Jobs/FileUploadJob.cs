using CcsSso.Core.DataMigrationJobScheduler.Contracts;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CcsSso.Core.DataMigrationJobScheduler.Jobs
{
  public class FileUploadJob : BackgroundService
  {
    private readonly DataMigrationAppSettings _appSettings;
    private readonly IFileUploadJobService _fileUploadJobService;
    private readonly ILogger _logger;
    private bool isRunning = false;

    public FileUploadJob(ILogger<FileUploadJob> logger, DataMigrationAppSettings appSettings, IServiceScopeFactory factory)
    {
      _logger = logger;
      _appSettings = appSettings;
      _fileUploadJobService = factory.CreateScope().ServiceProvider.GetRequiredService<IFileUploadJobService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.DataMigrationJobSettings.DataMigrationFileUploadJobFrequencyInMinutes * 60000;

        _logger.LogInformation("Job is Running:", isRunning);

        try
        {
          if (!isRunning)
          {
            isRunning = true;

            _logger.LogInformation("*******************************************************************************************");
            _logger.LogInformation("");
            _logger.LogInformation("DataMigration File Upload Job started at: {time}", DateTimeOffset.Now);

            await _fileUploadJobService.PerformFileUploadJobAsync();

            _logger.LogInformation("DataMigration File Upload Job finished at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("");
            _logger.LogInformation("*******************************************************************************************");

            isRunning = false;
          }
        }
        catch (Exception ex)
        {
          isRunning = false;
          _logger.LogError("Error While Running a Job"+ex.Message);
        }

        await Task.Delay(interval, stoppingToken);
      }
    }
  }
}
