using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class UserSetting : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }

    public UserSettingType UserSettingType { get; set; }

    public int UserSettingTypeId { get; set; }

    public string UserSettingValue { get; set; }
  }
}
