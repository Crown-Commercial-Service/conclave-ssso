using System;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class OrganisationGroupNameInfo
  {
    public string GroupName { get; set; }
  }

  public class OrganisationGroupRequestInfo : OrganisationGroupNameInfo
  {
    public OrganisationGroupRolePatchInfo RoleInfo { get; set; }

    public OrganisationGroupUserPatchInfo UserInfo { get; set; }
  }

  public class OrganisationGroupInfo : OrganisationGroupNameInfo
  {
    public int GroupId { get; set; }

    public string CreatedDate { get; set; }
  }

  public class OrganisationGroupResponseInfo : OrganisationGroupInfo
  {
    public string OrganisationId { get; set; }

    public List<GroupRole> Roles { get; set; }

    public List<GroupUser> Users { get; set; }
  }

  public class OrganisationGroupList
  {
    public string OrganisationId { get; set; }

    public List<OrganisationGroupInfo> GroupList { get; set; }
  }

  public class GroupRole
  {
    public int Id { get; set; }

    public string Name { get; set; }
  }

  public class GroupUser
  {
    public string UserId { get; set; }

    public string Name { get; set; }
  }

  public class OrganisationGroupRolePatchInfo
  {
    public List<int> AddedRoleIds { get; set; }

    public List<int> RemovedRoleIds { get; set; }
  }

  public class OrganisationGroupUserPatchInfo
  {
    public List<string> AddedUserIds { get; set; }

    public List<string> RemovedUserIds { get; set; }
  }
}
