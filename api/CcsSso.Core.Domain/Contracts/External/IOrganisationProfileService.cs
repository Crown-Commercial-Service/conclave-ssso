using CcsSso.Core.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IOrganisationProfileService
  {
    Task<string> CreateOrganisationAsync(OrganisationProfileInfo organisationProfileInfo);

    // Deletion has commented out since its not going to be exposed via the api at the moment
    // Task DeleteOrganisationAsync(string ciiOrganisationId);

    Task<OrganisationProfileInfo> GetOrganisationAsync(string ciiOrganisationId);

    Task<List<OrganisationGroups>> GetOrganisationGroupsAsync(string ciiOrganisationId);

    Task UpdateOrganisationAsync(string ciiOrganisationId, OrganisationProfileInfo organisationProfileInfo);
  }
}
