using CcsSso.Core.Domain.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ServiceOnboardingScheduler.Model
{
  public class OnBoardingAppSettings
  {
    public string DbConnection { get; set; }
    public bool ReportingMode { get; set; }
    public ApiSettings? SecurityApiSettings { get; set; }

    public ApiSettings? WrapperApiSettings { get; set; }
    public ApiSettings? LookupApiSettings { get; set; }
    public CiiSettings? CiiSettings { get; set; }

    public ScheduleJob? ScheduleJobSettings { get; set; }
    public OnBoardingDataDuration? OnBoardingDataDuration { get; set; }

    public string[]? CASDefaultRoles { get; set; }
    public string[]? SupplierRoles { get; set; }
   
    public int MaxNumbeOfRecordInAReport { get; set; }

    public Email EmailSettings { get; set; }

    public string? LogReportEmailId { get; set; }

    public OneTimeValidation? OneTimeValidation { get; set; }

  }


  public class ApiSettings
  {
    public string? ApiKey { get; set; }

    public string? Url { get; set; }
  }

  public class CiiSettings
  {
    public string? Url { get; set; }

    public string? Token { get; set; }
  }

  public class ScheduleJob
  {
    public int CASOnboardingJobScheduleInMinutes { get; set; }
  }

  public class OnBoardingDataDuration
  {
    public int CASOnboardingDurationInMinutes { get; set; }
  }

  public class Email
  {
    public string? ApiKey { get; set; }
    public string? FailedAutoValidationNotificationTemplateId { get; set; }

    public List<string> EmailIds { get; set; }

  }

  public class OneTimeValidation
  {
    public bool Switch { get; set; }
    public string StartDate { get; set; }

    public string EndDate { get; set; }

  }
}
