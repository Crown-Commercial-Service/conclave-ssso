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
using CcsSso.Dtos.Domain.Models;

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

		public async Task<List<OrganisationDto>> GetOrganisationDataAsync(OrganisationFilterCriteria organisationFilterCriteria, ResultSetCriteria resultSetCriteria)
		{
			var url = $"data?organisation-name={organisationFilterCriteria.OrganisationName}" +
									$"&exact-match-name={organisationFilterCriteria.IsExactMatchName}" +
									$"&include-all={organisationFilterCriteria.IncludeAll}" +
									$"&organisation-ids={string.Join(",", organisationFilterCriteria.OrganisationIds)}" +
									$"&is-match-name={organisationFilterCriteria.IsMatchName}" +
									$"&start-date={organisationFilterCriteria.StartDate}" +
									$"&end-date={organisationFilterCriteria.EndDate}" +
									$"&until-date-time={organisationFilterCriteria.UntilDateTime}" +
									$"&page-size={resultSetCriteria.PageSize}" +
									$"&current-page={resultSetCriteria.CurrentPage}" +
									$"&is-pagination={resultSetCriteria.IsPagination}";
			var result = await _wrapperApiService.GetAsync<OrganisationListResponse>(WrapperApi.Organisation, url, $"{CacheKeyConstant.OrganisationData}", "ERROR_RETRIEVING_ORGANISATION");
			return result.OrgList;
		}
	}
}
