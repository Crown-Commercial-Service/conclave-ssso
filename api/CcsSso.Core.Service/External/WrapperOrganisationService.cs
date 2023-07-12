using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Domain.Constants;
using CcsSso.DbModel.Entity;
using System.Collections.Generic;

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

	}
}
