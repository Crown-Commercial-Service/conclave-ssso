using CcsSso.Shared.Contracts;
using System;

namespace CcsSso.Shared.Services
{
  public class DateTimeService : IDateTimeService
  {
    public DateTime GetUTCNow()
    {
      return DateTime.UtcNow;
    }
  }
}
