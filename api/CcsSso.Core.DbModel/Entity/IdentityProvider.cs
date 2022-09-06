using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CcsSso.DbModel.Entity
{
  public class IdentityProvider : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string IdpUri { get; set; }

    public string IdpConnectionName { get; set; }

    public string IdpName { get; set; }

    public bool ExternalIdpFlag { get; set; }

    public List<IdamUserLogin> IdamUserLogins { get; set; }

    public int DisplayOrder { get; set; }
  }
}
