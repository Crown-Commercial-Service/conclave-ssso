using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationAuditEventService
  {
    Task<OrganisationAuditEventInfoListResponse> GetOrganisationAuditEventsListAsync(string ciiOrganisationId, ResultSetCriteria resultSetCriteria);
    
    Task CreateOrganisationAuditEventAsync(List<OrganisationAuditEventInfo> organisationAuditEventInfoList);

    Task<OrgAuditEventInfoServiceRoleGroupListResponse> GetOrganisationServiceRoleGroupAuditEventsListAsync(string ciiOrganisationId, ResultSetCriteria resultSetCriteria);
  }
}
