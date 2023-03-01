using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class OrganisationAuditEventInfo
  {
    public int OrganisationId { get; set; }

    public string SchemeIdentifier { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid GroupId { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public string Event { get; set; }

    public string Roles { get; set; }
  }

  public class AuditEventResponseBase 
  {
    public string OrganisationId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid GroupId { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public string Event { get; set; }

    public DateTime Date { get; set; }
  }

  public class OrganisationAuditEventResponseInfo : AuditEventResponseBase
  {
    public string Role { get; set; }

    public string RoleKey { get; set; }

    public string ServiceName { get; set; }
  }

  public class OrganisationAuditEventInfoListResponse : PaginationInfo
  {
    public List<OrganisationAuditEventResponseInfo> OrganisationAuditEventList { get; set; }
  }

  #region ServiceRoleGroup
  public class OrgAuditEventServiceRoleGroupResponseInfo : AuditEventResponseBase
  {
    public string Name { get; set; }

    public string Key { get; set; }
  }

  public class OrgAuditEventInfoServiceRoleGroupListResponse : PaginationInfo
  {
    public List<OrgAuditEventServiceRoleGroupResponseInfo> OrgAuditEventServiceRoleGroupList { get; set; }
  }
  #endregion
}
