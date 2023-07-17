using System.Collections.Generic;
using System.Threading.Tasks;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;

namespace CcsSso.Core.Domain.Contracts.External
{
  // #Auto validation
  public interface IWrapperOrganisationService
	{
		Task<OrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId);
		Task<List<OrganisationDto>> GetOrganisationDataAsync(OrganisationFilterCriteria organisationFilterCriteria, ResultSetCriteria resultSetCriteria);
	}
}
