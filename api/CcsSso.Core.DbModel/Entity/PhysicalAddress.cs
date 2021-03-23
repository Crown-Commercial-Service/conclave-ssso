using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class PhysicalAddress : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // [Required]
    public string StreetAddress { get; set; }

    // [Required]
    public string Locality { get; set; }

    // [Required]
    public string Region { get; set; }

    // [Required]
    public string PostalCode { get; set; }

    // [Required]
    public string CountryCode { get; set; }

    public string Uprn { get; set; }

    public ContactDetail ContactDetail { get; set; }

    [ForeignKey("ContactDetailId")]
    public int ContactDetailId { get; set; }
  }
}
