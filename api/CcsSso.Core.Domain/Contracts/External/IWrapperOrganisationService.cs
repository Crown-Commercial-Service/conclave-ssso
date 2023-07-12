using System.Collections.Generic;
using System.Threading.Tasks;
using CcsSso.Core.Domain.Dtos.External;

namespace CcsSso.Core.Domain.Contracts.External
{
  // #Auto validation
  public interface IWrapperOrganisationService
	{
		Task<OrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId);
	}
}
