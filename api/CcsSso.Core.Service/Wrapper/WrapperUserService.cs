using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Domain.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

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
		public async Task DeleteDelegatedUser(string userName, string ciiOrganisationId)
		{
			await _wrapperApiService.DeleteAsync(WrapperApi.User, $"delegate-user/terminate?user-id={userName}&delegated-organisation-id={ciiOrganisationId}", "ERROR_DELETING_DELEGATED_USER");
		}

		public async Task<int> CreateDelegationEmailNotificationLog(DelegationEmailNotificationLogInfo delegationEmailNotificationLogInfo)
		{
			return await _wrapperApiService.PostAsync<int>(WrapperApi.User, $"delegate-user/notification-log", delegationEmailNotificationLogInfo, "ERROR_CREATING_USER_DELEGATION_EMAIL_NOTIFICATION_LOG");
		}

		public async Task<bool> CreateDelegationAuditEvent(DelegationAuditEventRequestInfo delegationAuditEventRequestInfoList)
		{
			return await _wrapperApiService.PostAsync<bool>(WrapperApi.User, $"delegate-user/audit-event", delegationAuditEventRequestInfoList, "ERROR_CREATING_DELEGATION_AUDIT_EVENTS");
		}

		public async Task<List<DelegationUserDto>> GetDelegationLinkExpiredUsersAsync()
		{
			return await _wrapperApiService.GetAsync<List<DelegationUserDto>>(WrapperApi.User, $"delegate-user/link-expired", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_DELEGATION_LINK_EXPIRED_USERS");
		}

		public async Task<List<DelegationUserDto>> GetDelegationTerminatedUsersAsync()
		{
			return await _wrapperApiService.GetAsync<List<DelegationUserDto>>(WrapperApi.User, $"delegate-user/expired", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_DELEGATION_TERMINATED_USERS");
		}
		public async Task<List<DelegationUserDto>> GetUsersWithinExpiredNoticeAsync(string untilDate)
		{
			return await _wrapperApiService.GetAsync<List<DelegationUserDto>>(WrapperApi.User, $"delegate-user/expired?expiry-date={untilDate}", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_USERS_WITHIN_EXPIRED_NOTICE");
		}
		public async Task<List<string>> GetOrgAdminAsync(string ciiOrganisationId)
		{
			return await _wrapperApiService.GetAsync<List<string>>(WrapperApi.User, $"data/admin-email-ids?organisation-id={ciiOrganisationId}", $"{CacheKeyConstant.User}", "ERROR_RETRIEVING_ORGANISATION_ADMIN_USERS");
		}

    public async Task<UserAccessRolePendingRequestDetails> GetUserAccessRolePendingDetails(UserAccessRolePendingFilterCriteria criteria)
    {
			string url = "";
			if (criteria.Status != null)
			{
				url += "status=" + criteria.Status.ToString();
			}
			if(criteria.UserIds != null && criteria.UserIds.Any())
			{
				url += url.Length > 0 ? "&user-ids=" + string.Join(',', criteria.UserIds) : "user-ids=" + string.Join(',', criteria.UserIds);
			}
      return await _wrapperApiService.GetAsync<UserAccessRolePendingRequestDetails>(WrapperApi.User, $"approval/user-roles?{url}", $"{CacheKeyConstant.User}-USER_ACCESSROLE_PENDING", "ERROR_RETRIEVING_USER_ACCESSROLE_PENDING");
    }

    public async Task RemoveApprovalPendingRoles(string UserName, List<int> roleIds, UserPendingRoleStaus? status)
    {
      await _wrapperApiService.DeleteAsync(WrapperApi.User, $"approval/roles?user-id={UserName}&roles={string.Join(",", roleIds)}&status={(int)status}", "ERROR_DELETING_USER_ACCESS_ROLE_PENDING");
    }

    public async Task<UserListResponseInfo> GetUserByOrganisation(string CiiOrganisationId, UserFilterCriteria filter)
    {
      var url = $"{CiiOrganisationId}/users?search-string={filter.searchString}" +
                $"&delegated-only={filter.isDelegatedOnly}&delegated-expired-only={filter.isDelegatedExpiredOnly}" +
                $"&isAdmin={filter.isAdmin}&include-unverified-admin={filter.includeUnverifiedAdmin}&include-self={filter.includeSelf}&exclude-inactive={filter.excludeInactive}";

      var result = await _wrapperApiService.GetAsync<UserListResponseInfo>(WrapperApi.Organisation, url, $"{CacheKeyConstant.OrganisationUsers}", "ERROR_RETRIEVING_ORGANISATION_USERS");
      return result;
    }

    public async Task<List<UserToDeleteResponse>> GetInActiveUsers(string createdOnUtc)
    {
      return await _wrapperApiService.GetAsync<List<UserToDeleteResponse>>(WrapperApi.User, $"in-active?created-on={createdOnUtc}", $"{CacheKeyConstant.User}-TO-DELETE-{createdOnUtc}", "ERROR_RETRIEVING_USERS_TO_DELETE");
    }

    public async Task DeleteUserAsync(string userName)
    {
      await _wrapperApiService.DeleteAsync(WrapperApi.User, $"?user-id={userName}", "ERROR_DELETING_USER");
    }

    public async Task<bool> DeleteAdminUserAsync(string userName)
    {
      return await _wrapperApiService.DeleteAsync<bool>(WrapperApi.User, $"admin?user-id={userName}", "ERROR_DELETING_USER");
    }

		public async Task DeactivateUserAsync(string userName, DormantBy dormantBy)
		{
       await _wrapperApiService.PutAsync(WrapperApi.User, $"deactivation?user-id={userName}&dormant-by={dormantBy}",null,"ERROR_DEACTIVATING_USER");
    }
    public async Task<UserDetailsResponse> GetUserDetails(string userName)
    {
      userName = HttpUtility.UrlEncode(userName);
      return await _wrapperApiService.GetAsync<UserDetailsResponse>(WrapperApi.User, $"?user-id={userName}", $"UserDetails-{userName}", "ERROR_GETTING_USER_DETAILS");
    }
  }
}
