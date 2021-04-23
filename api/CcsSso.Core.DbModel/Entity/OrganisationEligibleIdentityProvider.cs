using CcsSso.DbModel.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DbModel.Entity
{
  public class OrganisationEligibleIdentityProvider: BaseEntity
  {
    public int Id { get; set; }

    public Organisation Organisation { get; set; }

    public int OrganisationId { get; set; }

    public IdentityProvider IdentityProvider { get; set; }

    public int IdentityProviderId { get; set; }

    public List<User> Users { get; set; }
  }
}
