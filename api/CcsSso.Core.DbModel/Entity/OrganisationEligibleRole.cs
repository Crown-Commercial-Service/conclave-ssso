using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class OrganisationEligibleRole : BaseEntity
  {
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

    public int CcsAccessRoleId { get; set; }
  }
}
