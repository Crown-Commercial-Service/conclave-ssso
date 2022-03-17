using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Dtos.Wrapper
{
  public class WrapperUserResponse
  {
    public string UserName { get; set; }

    public string OrganisationId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Title { get; set; }

    public UserResponseDetail Detail { get; set; }
  }

  public class UserResponseDetail
  {
    public int Id { get; set; }

    public List<UserIdentityProvider> IdentityProviders { get; set; }

    public List<GroupAccessRole> UserGroups { get; set; }

    public bool CanChangePassword { get; set; }

    public List<RolePermissionInfo> RolePermissionInfo { get; set; }
  }

  public class UserIdentityProvider
  {
    public int IdentityProviderId { get; set; }

    public string IdentityProviderDisplayName { get; set; }
  }

  public class RolePermissionInfo
  {
    public int RoleId { get; set; }

    public string RoleName { get; set; }

    public string RoleKey { get; set; }

    public string ServiceClientName { get; set; }
  }

  public class GroupAccessRole
  {
    public int GroupId { get; set; }

    public string AccessRole { get; set; }

    public string AccessRoleName { get; set; }

    public string Group { get; set; }

    public string ServiceClientName { get; set; }
  }

  public class WrapperUserRequest
  {
    public string OrganisationId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public int Title { get; set; }

    public UserRequestDetail Detail { get; set; }
  }

  public class UserRequestDetail
  {
    public List<int> GroupIds { get; set; }

    public List<int> RoleIds { get; set; }

    public List<int> IdentityProviderIds { get; set; }
  }

  public class WrapperUserEditResponseInfo
  {
    public string UserId { get; set; }
  }

  public class WrapperUserListInfo
  {
    public string Name { get; set; }

    public string UserName { get; set; }
  }

  public class WrapperUserListPaginationResponse
  {
    public string OrganisationId { get; set; }

    public List<WrapperUserListInfo> UserList { get; set; }

    public int CurrentPage { get; set; }

    public int PageCount { get; set; }

    public int RowCount { get; set; }
  }
}
