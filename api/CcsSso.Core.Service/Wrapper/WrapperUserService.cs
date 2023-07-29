using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Domain.Constants;
using CcsSso.DbModel.Entity;
using System.Collections.Generic;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.Wrapper;
using System.Linq;
//using CcsSso.Core.Domain.Dtos.External;
using System.Drawing;
using CcsSso.Core.DbModel.Entity;
using System;


namespace CcsSso.Core.Service.Wrapper
{
    // #Auto validation
    public class WrapperUserService : IWrapperUserService
	{
		private readonly IWrapperApiService _wrapperApiService;

		public WrapperUserService(IWrapperApiService wrapperApiService)
		{
			_wrapperApiService = wrapperApiService;

		}
		public async Task DeleteDelegatedUser(string userName, int organisationId)
		{
			await _wrapperApiService.DeleteAsync(WrapperApi.User, $"delegate-user/terminate?user-id={userName}&delegated-organisation-id={organisationId}", "ERROR_DELETING_DELEGATED_USER");
		}

		public async Task<int> CreateDelegationEmailNotificationLog(DelegationEmailNotificationLogInfo delegationEmailNotificationLogInfo)
		{
			return await _wrapperApiService.PostAsync<int>(WrapperApi.User, $"delegate-user/notification-logs", delegationEmailNotificationLogInfo, "ERROR_CREATING_USER_DELEGATION_EMAIL_NOTIFICATION_LOG");
		}

		public async Task<int> CreateDelegationAuditEvents(DelegationAuditEventRequestInfo delegationAuditEventRequestInfoList)
		{
			return await _wrapperApiService.PostAsync<int>(WrapperApi.User, $"delegate-user/audit-events", delegationAuditEventRequestInfoList, "ERROR_CREATING_DELEGATION_AUDIT_EVENTS");
		}

		public async Task<List<DelegationUserDto>> GetDelegationLinkExpiredUsersAsync()
		{
			return await _wrapperApiService.GetAsync<List<DelegationUserDto>>(WrapperApi.User, $"delegate-user/link-expired", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_DELEGATION_LINK_EXPIRED_USERS", false);
		}

		public async Task<List<DelegationUserDto>> GetDelegationTerminatedUsersAsync()
		{
			return await _wrapperApiService.GetAsync<List<DelegationUserDto>>(WrapperApi.User, $"delegate-user/end-dated", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_DELEGATION_TERMINATED_USERS", false);
		}
		public async Task<List<DelegationUserDto>> GetUsersWithinExpiredNoticeAsync(string untilDate)
		{
			return await _wrapperApiService.GetAsync<List<DelegationUserDto>>(WrapperApi.User, $"delegate-user/expired-notice?untilDate={untilDate}", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_USERS_WITHIN_EXPIRED_NOTICE", false);
		}
		public async Task<List<string>> GetOrgAdminAsync(int organisationId)
		{
			return await _wrapperApiService.GetAsync<List<string>>(WrapperApi.User, $"data/admin-email-ids?organisation-id={organisationId}", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_ORGANISATION_ADMIN_USERS", false);
		}
	}
}
