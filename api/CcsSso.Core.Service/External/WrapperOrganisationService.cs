using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Domain.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  // #Auto validation
  public class WrapperOrganisationService : IWrapperOrganisationService
	{
		private readonly IWrapperApiService _wrapperApiService;

		public WrapperOrganisationService(IWrapperApiService wrapperApiService)
		{
			_wrapperApiService = wrapperApiService;
			
		}

		public async Task<OrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId)
		{
			var result = await _wrapperApiService.GetAsync<OrganisationProfileResponseInfo>(WrapperApi.Organisation, $"internal/cii/{organisationId}", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION");
			return result;
		}

    public async Task<OrganisationProfileResponseInfo> GetOrganisationDetailsById(int organisationId)
    {
			var result = await _wrapperApiService.GetAsync<OrganisationProfileResponseInfo>(WrapperApi.Organisation, $"internal/{organisationId}", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION");
			return result;
    }

    public async Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId)
    {
			var result = await _wrapperApiService.GetAsync<List<OrganisationRole>>(WrapperApi.Organisation, $"{organisationId}/roles", $"{CacheKeyConstant.Organisation}-{organisationId}-ROLES", "ORGANISATION_ROLES_NOT_FOUND");
			return result;
    }

  }
}
