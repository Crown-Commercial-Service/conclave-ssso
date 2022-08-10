using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IOrganisationService
  {
    Task<string> RegisterAsync(OrganisationRegistrationDto organisationRegistrationDto);

    Task DeleteAsync(string ciiOrgId);

    Task<OrganisationDto> GetAsync(string id);

    Task<List<OrganisationDto>> GetByNameAsync(string name, bool isExact = true);

    Task<OrganisationListResponse> GetAllAsync(string orgName, ResultSetCriteria resultSetCriteria);

    Task<OrganisationUserListResponse> GetUsersAsync(string name, ResultSetCriteria resultSetCriteria);

    Task NotifyOrgAdminToJoinAsync(OrganisationJoinRequest organisationJoinRequest);

    int GetAffectedUsersByRemovedIdp(string organisationId, string idps);

  }
}
