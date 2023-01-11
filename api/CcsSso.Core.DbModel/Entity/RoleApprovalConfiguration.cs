using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.DbModel.Entity
{
  public class RoleApprovalConfiguration : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CcsAccessRoleId { get; set; }
        
    public CcsAccessRole CcsAccessRole { get; set; }

    public int LinkExpiryDurationInMinute { get; set; }

    public string NotificationEmails { get; set; }
  }
}
