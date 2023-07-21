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
  public class WrapperOrganisationService : IWrapperOrganisationService
  {
    private readonly IWrapperApiService _wrapperApiService;

    public WrapperOrganisationService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<AuditLogResponse> OrgAuditLog(DateTime startDate, int pageSize, int currentPage)
    {
      return await _wrapperApiService.GetAsync<AuditLogResponse>(WrapperApi.Organisation, $"data/audit?startDate={startDate}&page-size={pageSize}&current-page={currentPage}", "ERROR_GETTING_ORGANISATION_AUDIT_LOG");
    }
  }
}
