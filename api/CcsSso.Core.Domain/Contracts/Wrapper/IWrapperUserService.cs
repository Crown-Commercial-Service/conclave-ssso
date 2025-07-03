using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Dtos.Domain.Models;



namespace CcsSso.Core.Domain.Contracts.Wrapper
{
	// #Auto validation
	public interface IWrapperUserService
	{
		Task<bool> CreateDelegationAuditEvent(DelegationAuditEventRequestInfo delegationAuditEventRequestInfoList);

		Task<int> CreateDelegationEmailNotificationLog(DelegationEmailNotificationLogInfo delegationEmailNotificationLogInfo);

		Task DeleteDelegatedUser(string userName, string ciiOrganisationId);

		Task<List<DelegationUserDto>> GetDelegationLinkExpiredUsersAsync();

		Task<List<DelegationUserDto>> GetDelegationTerminatedUsersAsync();

		Task<List<DelegationUserDto>> GetUsersWithinExpiredNoticeAsync(string untilDate);

		Task<List<string>> GetOrgAdminAsync(string ciiOrganisationId);

    Task<UserAccessRolePendingRequestDetails> GetUserAccessRolePendingDetails(UserAccessRolePendingFilterCriteria criteria);
    Task RemoveApprovalPendingRoles(string UserName, List<int> roleIds, UserPendingRoleStaus? status);
    Task<UserListResponseInfo> GetUserByOrganisation(string organisationId, UserFilterCriteria filter);
    Task<List<UserToDeleteResponse>> GetInActiveUsers(string createdOnUtc);
    Task DeleteUserAsync(string userName, bool removeUser = false);
    Task<bool> DeleteAdminUserAsync(string userName);
		Task DeactivateUserAsync(string userName, DormantBy dormantBy);
		Task<UserDetailsResponse> GetUserDetails(string userName);
		Task<UserDataResponseInfo> GetUsersData(UserDataFilterCriteria userDataFilterCriteria);
  }
}
