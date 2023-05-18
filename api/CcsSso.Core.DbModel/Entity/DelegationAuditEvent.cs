using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Core.DbModel.Entity
{
  public class DelegationAuditEvent
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid GroupId { get; set; }

    public int UserId { get; set; }

    public DateTime? PreviousDelegationStartDate { get; set; }

    public DateTime? PreviousDelegationEndDate { get; set; }

    public DateTime? NewDelegationStartDate { get; set; }

    public DateTime? NewDelegationEndDate { get; set; }

    public string Roles { get; set; }

    public string EventType { get; set; }

    public DateTime ActionedOnUtc { get; set; }

    public string ActionedBy { get; set; }

    public string ActionedByUserName { get; set; }

    public string ActionedByFirstName { get; set; }

    public string ActionedByLastName { get; set; }
    
    public User User { get; set; }

  }
}
