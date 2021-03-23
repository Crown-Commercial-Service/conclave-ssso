using CcsSso.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserProfileRequestInfo
  {
    public string OrganisationId { get; set; }

    public string UserName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public UserTitle Title { get; set; }

    public List<int> GroupIds { get; set; }

    public int IdentityProviderId { get; set; }
  }

  public class UserProfileResponseInfo : UserProfileRequestInfo
  {
    public int Id { get; set; }

    public string IdentityProvider { get; set; }

    public string IdentityProviderDisplayName { get; set; }

    public List<GroupAccessRole> UserGroups { get; set; }

    public bool CanChangePassword { get; set; }
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
    public string AccessRole { get; set; }

    public string Group { get; set; }
  }
}
