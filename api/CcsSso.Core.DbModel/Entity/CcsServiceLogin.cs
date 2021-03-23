using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class CcsServiceLogin : BaseEntity
  {
    public int Id { get; set; }

    public CcsService CcsService { get; set; }

    public int CcsServiceId { get; set; }

    public IdamUserLogin IdamUserLogin { get; set; }

    public int IdamUserLoginId { get; set; }

    public bool TimedOut { get; set; }
  }
}
