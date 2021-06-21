using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Dtos.Wrapper
{
  public class WrapperContactResponse
  {
    public int ContactId { get; set; }

    public string ContactType { get; set; }

    public string ContactValue { get; set; }
  }

  public class WrapperContactPoint
  {
    public int ContactPointId { get; set; }

    public string ContactPointReason { get; set; }

    public string ContactPointName { get; set; }

    public List<WrapperContactResponse> Contacts { get; set; }
  }

  public class WrapperContactPointInfoList
  {
    public List<WrapperContactPoint> ContactPoints { get; set; }
  }

  public class WrapperOrganisationContactInfo : WrapperContactPoint
  {
    public OrganisationDetailInfo Detail { get; set; }
  }

  public class WrapperOrganisationContactInfoList : WrapperContactPointInfoList
  {
    public OrganisationDetailInfo Detail { get; set; }
  }

  public class WrapperUserContactInfo : WrapperContactPoint
  {
    public UserDetailInfo Detail { get; set; }
  }

  public class WrapperUserContactInfoList : WrapperContactPointInfoList
  {
    public UserDetailInfo Detail { get; set; }
  }

  public class WrapperOrganisationSiteContactInfo : WrapperContactPoint
  {
    public SiteDetailInfo Detail { get; set; }
  }

  public class WrapperOrganisationSiteContactInfoList : WrapperContactPointInfoList
  {
    public SiteDetailInfo Detail { get; set; }
  }

  public class OrganisationDetailInfo
  {
    public string OrganisationId { get; set; }
  }

  public class UserDetailInfo
  {
    public string UserId { get; set; }

    public string OrganisationId { get; set; }
  }

  public class SiteDetailInfo
  {
    public int SiteId { get; set; }

    public string OrganisationId { get; set; }
  }

  public class WrapperContactRequest
  {
    public string ContactType { get; set; }

    public string ContactValue { get; set; }
  }

  public class WrapperContactPointRequest
  {
    public int ContactPointId { get; set; }

    public string ContactPointReason { get; set; }

    public string ContactPointName { get; set; }

    public List<WrapperContactRequest> Contacts { get; set; }
  }
}
