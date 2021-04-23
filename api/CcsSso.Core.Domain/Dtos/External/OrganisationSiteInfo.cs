using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class OrganisationSiteInfo
  {
    public string SiteName { get; set; }

    public OrganisationAddress Address { get; set; }
  }

  public class OrganisationSite : OrganisationSiteInfo
  {
    public SiteDetail Details { get; set; }
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

  public class SiteDetail
  {
    public int SiteId { get; set; }
  }
}
