using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;

namespace CcsSso.Core.DbModel.Entity
{
  public class IdamUserLogin : BaseEntity
  {
    public int Id { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }

    public IdentityProvider IdentityProvider { get; set; }

    public int IdentityProviderId { get; set; }

    public string Location { get; set; }

    public int DeviceType { get; set; }

    public string ClientDevice { get; set; }

    public DateTime CcsLoginDateTime { get; set; }

    public DateTime CcsLogoutDateTime { get; set; }

    public bool LoginSuccessful { get; set; }

    public List<IdamUserLoginRole> IdamUserLoginRoles { get; set; }

    public List<CcsServiceLogin> CcsServiceLogins { get; set; }
  }
}
