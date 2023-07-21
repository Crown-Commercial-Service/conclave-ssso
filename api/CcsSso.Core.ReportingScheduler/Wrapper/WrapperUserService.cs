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

    public async Task<List<UserModel>> GetUserDetails(string listOfUserIds)
    {
      return await _wrapperApiService.GetAsync<List<UserModel>>(WrapperApi.User, $"?user-id={listOfUserIds}", "ERROR_GETTING_USER_DETAILS");
    }

    public async Task<AuditLogResponse> GetUserAuditLog(DateTime startDate, int pageSize,int currentPage)
    {
      return await _wrapperApiService.GetAsync<AuditLogResponse>(WrapperApi.User, $"data/audit?startDate={startDate}&page-size={pageSize}&current-page={currentPage}", "ERROR_GETTING_USER_AUDIT_LOG");
    }
  }
}
