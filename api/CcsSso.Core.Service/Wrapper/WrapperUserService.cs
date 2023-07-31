using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Domain.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    public async Task<UserAccessRolePendingRequestDetails> GetUserAccessRolePendingDetails(UserAccessRolePendingFilterCriteria criteria)
    {
      var payload = JsonConvert.SerializeObject(criteria);
      return await _wrapperApiService.GetAsync<UserAccessRolePendingRequestDetails>(WrapperApi.User, $"internal/approve/roles?{payload}", $"{CacheKeyConstant.User}-USER_ACCESSROLE_PENDING", "ERROR_RETRIEVING_USER_ACCESSROLE_PENDING");
    }

    public async Task DeleteUserAccessRolePending(List<int> roleIds)
    {
      var Ids = string.Join(",", roleIds);
      await _wrapperApiService.DeleteAsync<bool>(WrapperApi.User, $"internal/approve/roles?roleIds={Ids}", "ERROR_DELETING_USER_ACCESS_ROLE_PENDING");
    }

    public async Task<List<UserResponse>> GetUserByUserName(string UserName, int CreatedUserId)
    {
      return await _wrapperApiService.GetAsync<List<UserResponse>>(WrapperApi.User, $"{UserName}/{CreatedUserId}", $"{CacheKeyConstant.User}-{UserName}", "ERROR_RETRIEVING_USER");
    }

    public async Task<List<UserListForOrganisationInfo>> GetUsersByOrganisation(int organisationId, UserFilterCriteria filter)
    {
      var url = $"internal/organisation/{organisationId}?search-string={filter.searchString}" +
                $"&delegated-only={filter.isDelegatedOnly}&delegated-expired-only={filter.isDelegatedExpiredOnly}" +
                $"&isAdmin={filter.isAdmin}&include-unverified-admin={filter.includeUnverifiedAdmin}&include-self={filter.includeSelf}";

      var result = await _wrapperApiService.GetAsync<List<UserListForOrganisationInfo>>(WrapperApi.User, url, $"{CacheKeyConstant.OrganisationUsers}", "ERROR_RETRIEVING_ORGANISATION_USERS", false);
      return result;
    }

    public async Task<List<UserToDeleteResponse>> GetUsersToDelete(DateTime createdOnUtc)
    {
      return await _wrapperApiService.GetAsync<List<UserToDeleteResponse>>(WrapperApi.User, $"users-to-delete?createdOnUtc={createdOnUtc.ToString("yyyy-MM-dd")}", $"{CacheKeyConstant.User}-TO-DELETE-{createdOnUtc}", "ERROR_RETRIEVING_USERS_TO_DELETE");
    }

    public async Task DeleteUserAsync(string userName)
    {
      await _wrapperApiService.DeleteAsync(WrapperApi.User, $"/user-id={userName}", "ERROR_DELETING_USER");
    }
  }
}
