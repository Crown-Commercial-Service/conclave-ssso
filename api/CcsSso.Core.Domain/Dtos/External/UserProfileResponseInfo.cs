using CcsSso.Core.DbModel.Constants;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CcsSso.Core.Domain.Dtos.External
{

  public class UserDetail
  {
    public string UserName { get; set; }

    public string OrganisationId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Title { get; set; }

    public bool MfaEnabled { get; set; }

    public string Password { get; set; }

    public bool AccountVerified { get; set; }

    public bool SendUserRegistrationEmail { get; set; } = true;

    public string? OriginOrganisationName { get; set; }

    // #Auto validation
    public string? CompanyHouseId { get; set; }

  }

  public class UserRequestMain
  {
    public int Id { get; set; }

    public List<int> GroupIds { get; set; }

    public List<int> IdentityProviderIds { get; set; }
  }

  public class UserRequestDetail : UserRequestMain
  {
    public List<int> RoleIds { get; set; }
  }

  public class UserServiceRoleGroupRequestDetail : UserRequestMain
  {
    public List<int> ServiceRoleGroupIds { get; set; }
  }

  public class UserResponseMain
  {
    public int Id { get; set; }
    
    public bool CanChangePassword { get; set; }

    public List<UserIdentityProviderInfo> IdentityProviders { get; set; }
    // #Delegated
    public UserDelegationDetails[]? DelegatedOrgs { get; set; }
  }

  public class UserResponseDetail : UserResponseMain
  {
    public List<GroupAccessRole> UserGroups { get; set; }

    public List<RolePermissionInfo> RolePermissionInfo { get; set; }
  }

  public class UserServiceRoleGroupResponseDetail : UserResponseMain
  {
    public List<GroupAccessServiceRoleGroup> UserGroups { get; set; }

    public List<ServiceRoleGroupInfo> ServiceRoleGroupInfo { get; set; }
  }

  // #Delegated
  public class UserDelegationDetails
  {
    public string? DelegatedOrgId { get; set; }

    public string? DelegatedOrgName { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? DelegationAccepted { get; set; }
  }

  public class UserIdentityProviderInfo
  {
    public int IdentityProviderId { get; set; }

    public string IdentityProvider { get; set; }

    public string IdentityProviderDisplayName { get; set; }
  }

  public class RolePermissionInfo
  {
    public int RoleId { get; set; }

    public string RoleName { get; set; }

    public string RoleKey { get; set; }

    public string ServiceClientId { get; set; }

    public string ServiceClientName { get; set; }
  }

  public class ServiceRoleGroupInfo
  {
    [JsonPropertyOrder(-1)]
    public int Id { get; set; }

    [JsonPropertyOrder(-1)]
    public string Name { get; set; }

    [JsonPropertyOrder(-1)]
    public string Key { get; set; }
  }

  public class UserProfileEditRequestInfo : UserDetail
  {
    public UserRequestDetail Detail { get; set; }
  }

  public class UserProfileServiceRoleGroupEditRequestInfo : UserDetail
  {
    public UserServiceRoleGroupRequestDetail Detail { get; set; }
  }

  public class UserProfileResponseInfo : UserDetail
  {
    public UserResponseDetail Detail { get; set; }
  }

  public class UserProfileServiceRoleGroupResponseInfo : UserDetail
  {
    public UserServiceRoleGroupResponseDetail Detail { get; set; }
  }

  public class UserList
  {
    public string Name { get; set; }

    public string UserName { get; set; }

    public int? RemainingDays { get; set; }

    // #Delegated
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? OriginOrganisation { get; set; }

    public bool? DelegationAccepted { get; set; }

    public bool IsAdmin { get; set; } = false;

  }

  public class UserListInfo:UserList
  {
    public List<RolePermissionInfo> RolePermissionInfo { get; set; }
  }

  public class UserListWithServiceRoleGroupInfo:UserList
  {
    public List<ServiceRoleGroupInfo> ServicePermissionInfo { get; set; }
  }

  public class AdminUserListInfo
  {
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string Role { get; set; }
  }

  public class UserListResponse : PaginationInfo
  {
    public string OrganisationId { get; set; }

    public List<UserListInfo> UserList { get; set; }
  }

  public class UserListWithServiceGroupRoleResponse : PaginationInfo
  {
    public string OrganisationId { get; set; }
    public List<UserListWithServiceRoleGroupInfo> UserList { get; set; }
  }

  public class AdminUserListResponse : PaginationInfo
  {
    public string OrganisationId { get; set; }

    public List<AdminUserListInfo> AdminUserList { get; set; }
  }

  public class GroupAccessRole
  {
    public int GroupId { get; set; }

    public string AccessRole { get; set; }

    public string AccessRoleName { get; set; }

    public string Group { get; set; }

    public string ServiceClientId { get; set; }

    public string ServiceClientName { get; set; }
  }

  public class GroupAccessServiceRoleGroup
  {
    public int GroupId { get; set; }

    public int AccessServiceRoleGroupId { get; set; }

    public string AccessServiceRoleGroupName { get; set; }

    public string Group { get; set; }
  }


  public class UserEditResponseInfo
  {
    public string UserId { get; set; }

    public bool IsRegisteredInIdam { get; set; }
  }
  // #Delegated
  public class DelegatedUserProfileRequestInfo
  {
    public string UserName { get; set; }
    public DelegatedUserRequestDetail Detail { get; set; }
  }

  public class DelegatedUserProfileServiceRoleGroupRequestInfo
  {
    public string UserName { get; set; }
    public DelegatedUserServiceRoleGroupRequestDetail Detail { get; set; }
  }


  // #Delegated
  public class DelegatedUserRequestDetail
  {
    public string DelegatedOrgId { get; set; }

    public List<int> RoleIds { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
  }
  public class DelegatedUserServiceRoleGroupRequestDetail
  {
    public string DelegatedOrgId { get; set; }

    public List<int> ServiceRoleGroupIds { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
  }


  public class UserAccessRolePendingDetails
  {
    public int RoleId { get; set; }
    public string RoleKey { get; set; }

    public string RoleName { get; set; }

    public int Status { get; set; }

  }

  public class UserAccessRolePendingTokenDetails : UserAccessRolePendingDetails
  {
    public int Id { get; set; }
    public string UserName { get; set; }
  }

  public class UserRoleApprovalEditRequest
  {

    //TODO: check datatype
    public int[] PendingRoleIds { get; set; }

    // TODO: Check enum type for status
    public UserPendingRoleStaus Status { get; set; }
  }

  public class UserServiceRoleGroupPendingDetails
  {
    public int Id { get; set; }

    public string Key { get; set; }

    public string Name { get; set; }

    public int Status { get; set; }
  }

  public class UserAccessServiceRoleGroupPendingTokenDetails : UserServiceRoleGroupPendingDetails
  {
    public string UserName { get; set; }
  }
}
