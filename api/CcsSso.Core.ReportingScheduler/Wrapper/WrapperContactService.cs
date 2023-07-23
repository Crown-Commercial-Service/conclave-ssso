using CcsSso.Core.ReportingScheduler.Constants;
using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Core.ReportingScheduler.Wrapper.Contracts;
using CcsSso.Shared.Cache.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ReportingScheduler.Wrapper
{
  public class WrapperContactService : IWrapperContactService
  {
    private readonly IWrapperApiService _wrapperApiService;

    public WrapperContactService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<AuditLogResponse> GetContactAuditLog(DateTime startDate, int pageSize, int currentPage)
    {
      return await _wrapperApiService.GetAsync<AuditLogResponse>(WrapperApi.Contact, $"data/audit?startDate={startDate}&page-size={pageSize}&current-page={currentPage}", "ERROR_GETTING_CONTACT_AUDIT_LOG");
    }
  }
}
