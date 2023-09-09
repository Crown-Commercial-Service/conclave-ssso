using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserListResponseInfo : PaginationInfo
  {
    public string OrganisationId { get; set; }

    public List<UserListInfo> UserList { get; set; }
  }
}
