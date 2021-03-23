using CcsSso.DbModel.Entity;
using System.Collections.Generic;

namespace CcsSso.Core.DbModel.Entity
{
  public class ServicePermission: BaseEntity
  {
    public int Id { get; set; }

    public string ServicePermissionName { get; set; }

    public CcsService CcsService { get; set; }

    public int CcsServiceId { get; set; }

    public List<ServiceRolePermission> ServiceRolePermissions { get; set; }
  }
}
