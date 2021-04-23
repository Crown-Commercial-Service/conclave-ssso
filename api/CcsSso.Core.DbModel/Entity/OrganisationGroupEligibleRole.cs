using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class OrganisationGroupEligibleRole : BaseEntity
  {
    public int Id { get; set; }

    public OrganisationUserGroup OrganisationUserGroup { get; set; }

    public int OrganisationUserGroupId { get; set; }

    public OrganisationEligibleRole OrganisationEligibleRole { get; set; }

    public int OrganisationEligibleRoleId { get; set; }
  }
}
