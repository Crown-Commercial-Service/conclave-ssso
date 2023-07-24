using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  // #Auto validation
  public interface IWrapperOrganisationService
	{
		Task<OrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId);

    Task<OrganisationProfileResponseInfo> GetOrganisationDetailsById(int organisationId);

    Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId);
  }
}
