using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class ServiceRolePermission : BaseEntity
  {
    public int Id { get; set; }

    public ServicePermission ServicePermission { get; set; }

    public int ServicePermissionId { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

    public int CcsAccessRoleId { get; set; }
  }
}
