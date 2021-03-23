using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class OrganisationSiteInfo : OrganisationAddress
  {
    public string SiteName { get; set; }
  }

  public class OrganisationSite : OrganisationSiteInfo
  {
    public int SiteId { get; set; }
  }

  public class OrganisationSiteResponse : OrganisationSite
  {
    public string OrganisationId { get; set; }
  }

  public class OrganisationSiteInfoList
  {
    public string OrganisationId { get; set; }

    public  List<OrganisationSite> Sites { get; set; }
  }
}
