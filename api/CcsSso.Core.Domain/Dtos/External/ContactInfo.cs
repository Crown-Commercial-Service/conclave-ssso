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


  public class OrganisationContactInfoList
  {
    public OrganisationDetailInfo Detail { get; set; }

    public List<ContactResponseInfo> ContactPoints { get; set; }
  }
}
