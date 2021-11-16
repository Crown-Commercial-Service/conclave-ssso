using CcsSso.DbModel.Entity;
using System.Collections.Generic;

namespace CcsSso.Core.DbModel.Entity
{
  public class ExternalServiceRoleMapping : BaseEntity
  {
    public int Id { get; set; }

    public CcsService CcsService { get; set; }

    public int CcsServiceId { get; set; }

    public OrganisationEligibleRole OrganisationEligibleRole{ get; set; }

    public int OrganisationEligibleRoleId { get; set; }
  }
}
