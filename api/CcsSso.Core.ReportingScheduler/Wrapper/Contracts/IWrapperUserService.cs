﻿using CcsSso.Core.ReportingScheduler.Models;

namespace CcsSso.Core.ReportingScheduler.Wrapper.Contracts
{
  public interface IWrapperUserService
  {
    Task<AuditLogResponse> GetUserAuditLog(DateTime startDate, int pageSize, int currentPage);

    Task<UserNameListResponse> GetUserNames(string listOfUserIds);

    Task<List<UserReportDetail>> GetModifiedUsers(string modifiedDate);
  }
}