using CcsSso.Core.DbModel.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.DbModel.Entity
{
  public class User : BaseEntity
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string UserName { get; set; }

    public string JobTitle { get; set; }

    public int UserTitle { get; set; }

    public Party Party { get; set; }

    [ForeignKey("PartyId")]
    public int PartyId { get; set; }

    public bool MfaEnabled { get; set; }

    public bool AccountVerified { get; set; }

    public List<UserIdentityProvider> UserIdentityProviders { get; set; }

    public List<UserGroupMembership> UserGroupMemberships { get; set; }

    public List<UserSetting> UserSettings { get; set; }

    public List<UserAccessRole> UserAccessRoles { get; set; }

    public List<IdamUserLogin> IdamUserLogins { get; set; }

    public CcsService CcsService { get; set; }

    [ForeignKey("CcsServiceId")]
    public int? CcsServiceId { get; set; }
  }
}
