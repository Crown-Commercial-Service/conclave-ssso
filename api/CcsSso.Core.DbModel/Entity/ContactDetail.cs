using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System;

namespace CcsSso.DbModel.Entity
{
  public class ContactDetail : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public List<ContactPoint> ContactPoints { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime EffectiveTo { get; set; }

    public PhysicalAddress PhysicalAddress { get; set; }

    public List<VirtualAddress> VirtualAddresses { get; set; }
  }
}
