using Azure;
using CcsSso.Core.ReportingScheduler.Constants;
using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Core.ReportingScheduler.Wrapper.Contracts;

namespace CcsSso.Core.ReportingScheduler.Wrapper
{
  public class WrapperUserService : IWrapperUserService
  {
    private readonly IWrapperApiService _wrapperApiService;

    public WrapperUserService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<UserNameListResponse> GetUserNames(string listOfUserIds)
    {
      return await _wrapperApiService.GetAsync<UserNameListResponse>(WrapperApi.User, $"user-details/{listOfUserIds}", "ERROR_GETTING_USER_NAME_LIST");
    }

    public async Task<AuditLogResponse> GetUserAuditLog(DateTime startDate, int pageSize,int currentPage)
    {
      return await _wrapperApiService.GetAsync<AuditLogResponse>(WrapperApi.User, $"data/audit?startDate={startDate}&page-size={pageSize}&current-page={currentPage}", "ERROR_GETTING_USER_AUDIT_LOG");
    }

    public async Task<List<UserReportDetail>> GetModifiedUsers(string modifiedDate)
    {
      return await _wrapperApiService.GetAsync<List<UserReportDetail>>(WrapperApi.User, $"data/user-report?modifieddate={modifiedDate}", "ERROR_GETTING_MODIFIED_USERS");
    }
  }
}
