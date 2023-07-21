using CcsSso.Core.ReportingScheduler.Models;

namespace CcsSso.Core.ReportingScheduler.Wrapper.Contracts
{
    public interface IWrapperContactService
    {
        Task<AuditLogResponse> ContactAuditLog(DateTime startDate, int pageSize, int currentPage);
    }
}