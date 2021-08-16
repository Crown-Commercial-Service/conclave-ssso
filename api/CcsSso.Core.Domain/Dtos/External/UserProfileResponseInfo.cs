using CcsSso.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{

  public class UserDetail
  {
    public string UserName { get; set; }

    public string OrganisationId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public UserTitle? Title { get; set; }

    public bool MfaEnabled { get; set; }
  }

  public class UserRequestDetail
  {
    public int Id { get; set; }

    public List<int> GroupIds { get; set; }

    public List<int> RoleIds { get; set; }

    public int IdentityProviderId { get; set; }
  }

  public class UserResponseDetail
  {
    public int Id { get; set; }

    public int IdentityProviderId { get; set; }

    public string IdentityProvider { get; set; }

    public string IdentityProviderDisplayName { get; set; }

    public List<GroupAccessRole> UserGroups { get; set; }

    public bool CanChangePassword { get; set; }

    public List<RolePermissionInfo> RolePermissionInfo { get; set; }
  }

  public class RolePermissionInfo
  {
    public int RoleId { get; set; }

    public string RoleName { get; set; }

    public string RoleKey { get; set; }

    public string ServiceClientId { get; set; }

    public string ServiceClientName { get; set; }
  }

  public class UserProfileEditRequestInfo : UserDetail
  {
    public UserRequestDetail Detail { get; set; }
  }

  public class UserProfileResponseInfo : UserDetail
  {
    public UserResponseDetail Detail { get; set; }
  }

  public class UserListInfo
  {
    public string Name { get; set; }

    public string UserName { get; set; }
  }

  public class UserListResponse : PaginationInfo
  {
    public string OrganisationId { get; set; }

    public List<UserListInfo> UserList { get; set; }
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

  public class UserEditResponseInfo
  {
    public string UserId { get; set; }

    public bool IsRegisteredInIdam { get; set; }
  }
}
