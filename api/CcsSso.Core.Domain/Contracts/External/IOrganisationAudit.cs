using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationAuditService
  {
    Task<OrganisationAuditInfoListResponse> GetAllAsync(ResultSetCriteria resultSetCriteria, OrganisationAuditFilterCriteria organisationAuditFilterCriteria);
    Task CreateOrganisationAuditAsync(List<OrganisationAuditInfo> organisationAuditList);
    Task CreateOrganisationAuditAsync(OrganisationAuditInfo organisationAudit);
    Task UpdateOrganisationAuditAsync(OrganisationAuditInfo organisationAuditInfo);
  }
}
