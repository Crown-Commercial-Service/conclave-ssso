using CcsSso.DbModel.Entity;
using System.Collections.Generic;

namespace CcsSso.Core.DbModel.Entity
{
  public class CcsService : BaseEntity
  {
    public int Id { get; set; }

    public string ServiceName { get; set; }

    public string Description { get; set; }

    public string ServiceCode { get; set; }

    public string ServiceUrl { get; set; }

    public string ServiceClientId { get; set; }

    public long TimeOutLength { get; set; }

    public bool GlobalLevelOrganisationAccess { get; set; }

    public bool ActivateOrganisations { get; set; }

    public List<ServicePermission> ServicePermissions { get; set; }

    public List<CcsServiceLogin> CcsServiceLogins { get; set; }

    public List<ExternalServiceRoleMapping> ExternalServiceRoleMappings { get; set; }

    public List<Organisation> CreatedOrganisations { get; set; }

    public List<User> CreatedUsers { get; set; }
  }
}
