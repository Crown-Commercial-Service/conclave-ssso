using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class CcsServiceRoleMapping
  {
    public int Id { get; set; }

    public CcsServiceRoleGroup CcsServiceRoleGroup { get; set; }

    public int CcsServiceRoleGroupId { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

    public int CcsAccessRoleId { get; set; }

  }
}