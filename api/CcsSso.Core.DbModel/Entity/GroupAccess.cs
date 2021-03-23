using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class GroupAccess : BaseEntity
  {
    public int Id { get; set; }

    public OrganisationUserGroup OrganisationUserGroup { get; set; }

    public int OrganisationUserGroupId { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

    public int CcsAccessRoleId { get; set; }
  }
}
