using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class DelegationEmailNotificationLog
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int  UserId { get; set; }

    public User User { get; set; }

    public DateTime DelegationEndDate { get; set; }

    public DateTime NotifiedOnUtc { get; set; }

  }
}
