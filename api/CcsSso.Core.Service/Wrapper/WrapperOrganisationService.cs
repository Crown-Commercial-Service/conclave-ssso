using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Domain.Constants;
using CcsSso.DbModel.Entity;
using System.Collections.Generic;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.Wrapper;


namespace CcsSso.Core.Service.Wrapper
{
    // #Auto validation
    public class WrapperOrganisationService : IWrapperOrganisationService
	{
		private readonly IWrapperApiService _wrapperApiService;

		public WrapperOrganisationService(IWrapperApiService wrapperApiService)
		{
			_wrapperApiService = wrapperApiService;

		}

		public async Task<WrapperOrganisationProfileResponseInfo> GetOrganisationAsync(string organisationId)
		{
			var result = await _wrapperApiService.GetAsync<WrapperOrganisationProfileResponseInfo>(WrapperApi.Organisation, $"internal/cii/{organisationId}", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION",false);
			return result;
		}

		public async Task<OrganisationListResponseInfo> GetOrganisationDataAsync(OrganisationFilterCriteria organisationFilterCriteria, ResultSetCriteria resultSetCriteria)
		{
			var url = $"data?organisation-name={organisationFilterCriteria.OrganisationName}" +
															$"&exact-match-name={organisationFilterCriteria.IsExactMatchName}" +
															$"&include-all={organisationFilterCriteria.IncludeAll}" +
															$"&organisation-ids={string.Join(",", organisationFilterCriteria.OrganisationIds)}" +
															$"&is-match-name={organisationFilterCriteria.IsMatchName}" +
															$"&start-date={organisationFilterCriteria.StartDate}" +
															$"&end-date={organisationFilterCriteria.EndDate}" +
															$"&until-date-time={organisationFilterCriteria.UntilDateTime}" +
															$"&PageSize={resultSetCriteria.PageSize}" +
															$"&CurrentPage={resultSetCriteria.CurrentPage}" +
															$"&IsPagination={resultSetCriteria.IsPagination}";
			var result = await _wrapperApiService.GetAsync<OrganisationListResponseInfo>(WrapperApi.Organisation, url, $"{CacheKeyConstant.OrganisationData}", "ERROR_RETRIEVING_ORGANISATION_DATA",false);
			return result;
		}
	}
}
