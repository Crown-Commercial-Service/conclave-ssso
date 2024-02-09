using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace CcsSso.Core.Domain.Contracts.Wrapper
{
  // #Auto validation
  public interface IWrapperOrganisationService
  {
    Task<WrapperOrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId);
    Task<OrganisationListResponseInfo> GetOrganisationDataAsync(OrganisationFilterCriteria organisationFilterCriteria, ResultSetCriteria resultSetCriteria);
    Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId);
    Task<List<InactiveOrganisationResponse>> GetInactiveOrganisationAsync(string CreatedOnUtc);
    Task DeleteOrganisationAsync(string organisationId);
    Task<bool> UpdateOrganisationAuditList(WrapperOrganisationAuditInfo organisationAuditInfo);
    Task CreateOrganisationAuditEventAsync(List<WrapperOrganisationAuditEventInfo> organisationAuditEventInfoList);
    Task ActivateOrganisationByUser(string userId);
    //Task<List<UserListForOrganisationInfo>> GetUserByOrganisation(int organisationId, UserFilterCriteria filter);
    Task<OrganisationContactInfoList> GetOrganisationContactsList(string organisationId, string contactType = null, ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All);
  }
}
