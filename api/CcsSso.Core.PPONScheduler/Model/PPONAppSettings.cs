using CcsSso.Core.Domain.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.PPONScheduler.Model
{
  public class PPONAppSettings
  {
    public string DbConnection { get; set; }

    public ApiSettings? PPONApiSettings { get; set; }

    public CiiSettings? CiiSettings { get; set; }

    public ScheduleJob? ScheduleJobSettings { get; set; }

    public OneTimeJob? OneTimeJobSettings { get; set; }

  }
  public class ApiSettings
  {
    public string? Key { get; set; }

    public string? Url { get; set; }
  }

  public class CiiSettings
  {
    public string? Url { get; set; }

    public string? Token { get; set; }
  }

  public class ScheduleJob
  {
    public int ScheduleInMinutes { get; set; }
    public int DataDurationInMinutes { get; set; }
  }

  public class OneTimeJob
  {
    public bool Switch { get; set; }

    public string StartDate { get; set; }

    public string EndDate { get; set; }
  }
}
