using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ReportingScheduler.Models
{
  public class ModifiedOrgContactInfo
  {
    public int ContactPointId { get; set; }
    public int PartyId { get; set; }
    public int ContactDetailId { get; set; }
    public int OrganisationId { get; set; }
    public string CiiOrgId { get; set; }
  }

  public class ModifiedUserContactInfo
  {
    public int ContactPointId { get; set; }
    public int PartyId { get; set; }
    public int ContactDetailId { get; set; }
    public string UserName { get; set; }
  }

  public class ModifiedSiteContactInfo
  {
    public int ContactPointId { get; set; }
    public int SiteContactId { get; set; }
    public int OrganisationId { get; set; }
    public string CiiOrgId { get; set; }
  }
}
