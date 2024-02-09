using CcsSso.Core.ReportingScheduler.Models;

namespace CcsSso.Core.ReportingScheduler.Wrapper.Contracts
{
    public interface IWrapperContactService
    {
        Task<AuditLogResponse> GetContactAuditLog(DateTime startDate, int pageSize, int currentPage);

  }
}