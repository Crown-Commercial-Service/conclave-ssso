using CcsSso.Shared.Domain;

namespace CcsSso.Core.ReportingScheduler.Models
{
  public class AppSettings
  {
    public string? DbConnection { get; set; }
    public ApiConfig? SecurityApiSettings { get; set; }
    public ApiConfig? WrapperApiSettings { get; set; }
    public ScheduleJob? ScheduleJobSettings { get; set; }
    public ReportDataDuration? ReportDataDurations { get; set; }
    public S3Configuration? S3Configuration { get; set; }
    public AzureBlobConfiguration? AzureBlobConfiguration { get; set; }
    public int MaxNumbeOfRecordInAReport { get; set; }
    public bool WriteCSVDataInLog { get; set; }
  }

  public class ApiConfig
  {
    public string? ApiKey { get; set; }
    public string? Url { get; set; }
  }

  public class ScheduleJob
  {
    public int UserReportingJobScheduleInMinutes { get; set; }
    public int OrganisationReportingJobScheduleInMinutes { get; set; }
    public int ContactReportingJobScheduleInMinutes { get; set; }
    public int AuditLogReportingJobScheduleInMinutes { get; set; }

  }

  public class ReportDataDuration
  {
    public int UserReportingDurationInMinutes { get; set; }
    public int OrganisationReportingDurationInMinutes { get; set; }
    public int ContactReportingDurationInMinutes { get; set; }
    public int AuditLogReportingDurationInMinutes { get; set; }

   }

  public class S3Configuration
  {
    public string AccessKeyId { get; set; }
    public string AccessSecretKey { get; set; }
    public string ServiceUrl { get; set; }
    public string BucketName { get; set; }

  }

}
