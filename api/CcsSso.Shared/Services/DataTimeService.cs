using CcsSso.Shared.Contracts;
using System;

namespace CcsSso.Shared.Services
{
  public class DataTimeService : IDataTimeService
  {
    public DateTime GetUTCNow()
    {
      return DateTime.UtcNow;
    }
  }
}
