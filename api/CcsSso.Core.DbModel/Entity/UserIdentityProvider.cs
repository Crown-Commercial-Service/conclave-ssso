using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class UserIdentityProvider : BaseEntity
  {
    public int Id { get; set; }

    public OrganisationEligibleIdentityProvider OrganisationEligibleIdentityProvider { get; set; }

    public int OrganisationEligibleIdentityProviderId { get; set; }

    public User User { get; set; }

    public int UserId { get; set; }
  }
}
