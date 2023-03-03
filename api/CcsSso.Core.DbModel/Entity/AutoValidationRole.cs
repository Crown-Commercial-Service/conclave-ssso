using CcsSso.DbModel.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Core.DbModel.Entity
{
  public class AutoValidationRole
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CcsAccessRoleId { get; set; }

    public bool IsSupplier { get; set; }

    public bool IsBuyerSuccess { get; set; }

    public bool IsBuyerFailed { get; set; }

    public bool IsBothSuccess { get; set; }

    public bool IsBothFailed { get; set; }

    public bool AssignToOrg { get; set; }

    public bool AssignToAdmin { get; set; }

    public CcsAccessRole CcsAccessRole { get; set; }

  }
}
