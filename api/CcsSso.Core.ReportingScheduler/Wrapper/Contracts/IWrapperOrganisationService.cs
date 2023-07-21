using CcsSso.Core.ReportingScheduler.Models;

namespace CcsSso.Core.ReportingScheduler.Wrapper.Contracts
{
    public interface IWrapperOrganisationService
    {
        Task<AuditLogResponse> OrgAuditLog(DateTime startDate, int pageSize, int currentPage);
    }
}