using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Dtos.Domain.Models;



namespace CcsSso.Core.Domain.Contracts.Wrapper
{
	// #Auto validation
	public interface IWrapperUserService
	{
		Task<int> CreateDelegationAuditEvents(DelegationAuditEventRequestInfo delegationAuditEventRequestInfoList);

		Task<int> CreateDelegationEmailNotificationLog(DelegationEmailNotificationLogInfo delegationEmailNotificationLogInfo);

		Task DeleteDelegatedUser(string userName, int organisationId);

		Task<List<DelegationUserDto>> GetDelegationLinkExpiredUsersAsync();

		Task<List<DelegationUserDto>> GetDelegationTerminatedUsersAsync();

		Task<List<DelegationUserDto>> GetUsersWithinExpiredNoticeAsync(string untilDate);

		Task<List<string>> GetOrgAdminAsync(int organisationId);
	}
}
