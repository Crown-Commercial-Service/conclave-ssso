using CcsSso.Core.DbModel.Constants;
using System.Collections.Generic;

namespace CcsSso.Domain.Dtos.External
{
  public class ContactRequestDetail
  {
    public string ContactType { get; set; }

    public string ContactValue { get; set; }
  }

  public class ContactResponseDetail : ContactRequestDetail
  {
    public int ContactId { get; set; }
  }

  public class ContactPointInfo
  {
    public string ContactPointReason { get; set; }

    public string ContactPointName { get; set; }
  }

  public class ContactRequestInfo : ContactPointInfo
  {
    public List<ContactRequestDetail> Contacts { get; set; }
  }

  public class ContactResponseInfo : ContactPointInfo
  {
    public int ContactPointId { get; set; }

    public int OriginalContactPointId { get; set; }

    public AssignedContactType AssignedContactType { get; set; }

    public List<ContactResponseDetail> Contacts { get; set; }
  }

  public class OrganisationDetailInfo
  {
    public string OrganisationId { get; set; }
  }

  public class OrganisationContactInfo : ContactResponseInfo
  {
    public OrganisationDetailInfo Detail { get; set; }
  }

  public class OrganisationContactInfoList
  {
    public OrganisationDetailInfo Detail { get; set; }

    public List<ContactResponseInfo> ContactPoints { get; set; }
  }

  public class UserDetailInfo
  {
    public string UserId { get; set; }

    public string OrganisationId { get; set; }
  }

  public class UserContactInfo : ContactResponseInfo
  {
    public UserDetailInfo Detail { get; set; }
  }

  public class UserContactInfoList
  {
    public UserDetailInfo Detail { get; set; }

    public List<ContactResponseInfo> ContactPoints { get; set; }
  }

  public class SiteDetailInfo
  {
    public int SiteId { get; set; }

    public string OrganisationId { get; set; }
  }

  public class OrganisationSiteContactInfo : ContactResponseInfo
  {
    public SiteDetailInfo Detail { get; set; }
  }

  public class OrganisationSiteContactInfoList
  {
    public SiteDetailInfo Detail { get; set; }

    public List<ContactResponseInfo> ContactPoints { get; set; }
  }

  public class ContactReasonInfo
  {
    public string Key { get; set; }
    public string Value { get; set; }
  }
}
