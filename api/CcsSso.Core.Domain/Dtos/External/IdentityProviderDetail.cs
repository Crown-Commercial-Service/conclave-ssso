using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class IdentityProviderDetail
  {
    public int Id { get; set; }

    public string ConnectionName { get; set; }

    public string Name { get; set; }
  }

  public class OrgIdentityProviderSummary
  {
    public string CiiOrganisationId { get; set; }

    public List<OrgIdentityProvider> ChangedOrgIdentityProviders { get; set; }
  }

  public class OrgIdentityProvider
  {
    public int Id { get; set; }

    public string ConnectionName { get; set; }

    public string Name { get; set; }

    public bool Enabled { get; set; }
  }
}
