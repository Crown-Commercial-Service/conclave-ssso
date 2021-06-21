using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class AuditLoginService : IAuditLoginService
  {

    private readonly IDataContext _dataContext;
    private readonly RequestContext _requestContext;
    private readonly IDateTimeService _dateTimeService;
    public AuditLoginService(IDataContext dataContext, RequestContext requestContext, IDateTimeService dateTimeService)
    {
      _dataContext = dataContext;
      _requestContext = requestContext;
      _dateTimeService = dateTimeService;
    }

    public async Task CreateLogAsync(string eventName, string applicationName, string referenceData)
    {
      var auditLog = new AuditLog
      {
        Event = eventName,
        Application = applicationName,
        ReferenceData = referenceData,
        UserId = _requestContext.UserId,
        IpAddress = _requestContext.IpAddress,
        Device = _requestContext.Device,
        EventTimeUtc = _dateTimeService.GetUTCNow()
      };

      _dataContext.AuditLog.Add(auditLog);

      await _dataContext.SaveChangesAsync();
    }
  }
}
