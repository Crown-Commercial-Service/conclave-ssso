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

  public class OrganisationAuditEventResponseInfo
  {
    public string OrganisationId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid GroupId { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public string Event { get; set; }

    public string Role { get; set; }

    public string RoleKey { get; set; }

    public DateTime Date { get; set; }
  }

  public class OrganisationAuditEventInfoListResponse : PaginationInfo
  {
    public List<OrganisationAuditEventResponseInfo> OrganisationAuditEventList { get; set; }
  }
}
