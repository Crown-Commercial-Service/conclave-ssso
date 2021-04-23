using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Jobs
{
  public class AppSettings
  {
    public string DbConnection { get; set; }

    public ScheduleJobSettings ScheduleJobSettings { get; set; }

    public CiiSettings CiiSettings { get; set; }

    public SecurityApiSettings SecurityApiSettings { get; set; }
  }

  public class CiiSettings
  {
    public string Url { get; set; }

    public string Token { get; set; }
  }

  public class ScheduleJobSettings
  {
    public int JobSchedulerExecutionFrequencyInMinutes { get; set; }

    public int OrganizationRegistrationExpiredThresholdInMinutes { get; set; }
  }

  public class SecurityApiSettings
  {
    public string ApiKey { get; set; }

    public string Url { get; set; }
  }

  public class ScheduleJobSettingsVault
  {
    public string JobSchedulerExecutionFrequencyInMinutes { get; set; }

    public string OrganizationRegistrationExpiredThresholdInMinutes { get; set; }
  }
}
