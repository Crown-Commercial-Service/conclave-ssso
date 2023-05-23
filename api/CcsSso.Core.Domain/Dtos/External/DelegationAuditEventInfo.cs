using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class DelegationAuditEventInfo
  {
    public Guid GroupId { get; set; }

    public int UserId { get; set; }

    public DateTime? PreviousDelegationStartDate { get; set; }

    public DateTime? PreviousDelegationEndDate { get; set; }

    public DateTime? NewDelegationStartDate { get; set; }

    public DateTime? NewDelegationEndDate { get; set; }

    public string Roles { get; set; }

    public string EventType { get; set; }

    public DateTime ActionedOnUtc { get; set; }

    public string ActionedBy { get; set; }

    public string ActionedByUserName { get; set; }

    public string ActionedByFirstName { get; set; }

    public string ActionedByLastName { get; set; }
  }

  public class DelegationAuditEventResponseBase
  {
    public Guid GroupId { get; set; }

    public DateTime? PreviousDelegationStartDate { get; set; }

    public DateTime? PreviousDelegationEndDate { get; set; }

    public DateTime? NewDelegationStartDate { get; set; }

    public DateTime? NewDelegationEndDate { get; set; }

    public string EventType { get; set; }

    public DateTime ActionedOnUtc { get; set; }

    public string ActionedBy { get; set; }

    public string ActionedByUserName { get; set; }

    public string ActionedByFirstName { get; set; }

    public string ActionedByLastName { get; set; }
  }

  public class DelegationAuditEventResponseInfo : DelegationAuditEventResponseBase
  {
    public string Role { get; set; }

    public string RoleKey { get; set; }

    public string ServiceName { get; set; }
  }

  public class DelegationAuditEventInfoListResponse : PaginationInfo
  {
    public List<DelegationAuditEventResponseInfo> DelegationAuditEventList { get; set; }
  }
  
  public class DelegationAuditEventServiceRoleGroupResponseInfo : DelegationAuditEventResponseBase
  {
    public string Name { get; set; }

    public string Key { get; set; }
  }

  public class DelegationAuditEventoServiceRoleGroupInfListResponse : PaginationInfo
  {
    public List<DelegationAuditEventServiceRoleGroupResponseInfo> DelegationAuditEventServiceRoleGroupList { get; set; }
  }

}
