using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Dtos.Wrapper
{
  public class WrapperOrganisationSite
  {
    public WrapperSiteDetail Details { get; set; }

    public string SiteName { get; set; }

    public OrganisationAddress Address { get; set; }
  }

  public class WrapperOrganisationSiteResponse : WrapperOrganisationSite
  {
    public string OrganisationId { get; set; }
  }

  public class WrapperOrganisationSiteInfoList
  {
    public string OrganisationId { get; set; }

    public List<WrapperOrganisationSite> Sites { get; set; }
  }

  public class WrapperSiteDetail
  {
    public int SiteId { get; set; }
  }
}
