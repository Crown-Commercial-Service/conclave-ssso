using CcsSso.DbModel.Entity;
using System.Collections.Generic;

namespace CcsSso.Core.DbModel.Entity
{
  public class OrganisationEligibleRole : BaseEntity
  {
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

    public int CcsAccessRoleId { get; set; }

    public bool MfaEnabled { get; set; }

    public List<ExternalServiceRoleMapping> ExternalServiceRoleMappings { get; set; }
  }
}
