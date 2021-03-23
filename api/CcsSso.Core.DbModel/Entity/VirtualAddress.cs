using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class VirtualAddress : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string VirtualAddressValue { get; set; }

    public VirtualAddressType VirtualAddressType { get; set; }

    public int VirtualAddressTypeId { get; set; }

    public ContactDetail ContactDetail { get; set; }

    [ForeignKey("ContactDetailId")]
    public int ContactDetailId { get; set; }
  }
}
