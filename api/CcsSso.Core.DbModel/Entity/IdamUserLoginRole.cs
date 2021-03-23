using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class IdamUserLoginRole : BaseEntity
  {
    public int Id { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

    public int CcsAccessRoleId { get; set; }
  }
}
