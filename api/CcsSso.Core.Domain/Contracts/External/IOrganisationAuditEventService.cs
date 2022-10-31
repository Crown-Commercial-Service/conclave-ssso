using CcsSso.Core.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationAuditEventService
  {
    Task<OrganisationAuditEventInfoListResponse> GetOrganisationAuditEventsListAsync(int organisationId, ResultSetCriteria resultSetCriteria);
    Task CreateOrganisationAuditEventAsync(List<OrganisationAuditEventInfo> organisationAuditEventInfoList);
  }
}
