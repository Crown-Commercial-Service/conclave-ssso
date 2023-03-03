using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Core.DbModel.Entity
{
  public class OrganisationAuditEvent
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int OrganisationId { get; set; }

    public string SchemeIdentifier { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid GroupId { get; set; }

    public DateTime Date { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public string Event { get; set; }

    public string Roles { get; set; }

    public Organisation Organisation { get; set; }

  }
}
