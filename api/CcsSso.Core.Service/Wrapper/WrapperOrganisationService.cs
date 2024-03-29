﻿using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Dtos.External;
using CcsSso.Shared.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
			var result = await _wrapperApiService.GetAsync<WrapperOrganisationProfileResponseInfo>(WrapperApi.Organisation, $"{organisationId}", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION");
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
			var result = await _wrapperApiService.GetAsync<OrganisationListResponseInfo>(WrapperApi.Organisation, url, $"{CacheKeyConstant.OrganisationData}", "ERROR_RETRIEVING_ORGANISATION_DATA");
			return result;
		}

    public async Task<OrganisationContactInfoList> GetOrganisationContactsList(string organisationId, string contactType = null, ContactAssignedStatus contactAssignedStatus = ContactAssignedStatus.All)
    {
      return await _wrapperApiService.GetAsync<OrganisationContactInfoList>(WrapperApi.Organisation, $"{organisationId}/contacts", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION_USERS");
    }
    public async Task ActivateOrganisationByUser(string userId)
    {
      await _wrapperApiService.PostAsync<Task>(WrapperApi.Organisation, $"activation-by-user/{userId}", null, "ERROR_ACTIVATING_ORGANISATION_BY_USER_ID");
    }

    public async Task CreateOrganisationAuditEventAsync(List<WrapperOrganisationAuditEventInfo> organisationAuditEventInfoList)
    {
      await _wrapperApiService.PostAsync<Task>(WrapperApi.Organisation, "audit-events", organisationAuditEventInfoList, "ERROR_CREATING_ORGANISATION_AUDIT_EVENT_LOG");
    }

    public async Task DeleteOrganisationAsync(string organisationId)
    {
      await _wrapperApiService.DeleteAsync<Task>(WrapperApi.OrganisationDelete, $"{organisationId}", "ERROR_DELETING_ORGANISATION");
    }

    public async Task<List<InactiveOrganisationResponse>> GetInactiveOrganisationAsync(string CreatedOnUtc)
    {
      return await _wrapperApiService.GetAsync<List<InactiveOrganisationResponse>>(WrapperApi.Organisation, $"in-active?created-on={CreatedOnUtc}", $"{CacheKeyConstant.Organisation}-{CreatedOnUtc}", "ERROR_RETRIEVING_EXPIRED_ORGANISATION");
    }

    public async Task<List<OrganisationRole>> GetOrganisationRoles(string organisationId)
    {
      var result = await _wrapperApiService.GetAsync<List<OrganisationRole>>(WrapperApi.Organisation, $"{organisationId}/roles", $"{CacheKeyConstant.Organisation}-{organisationId}-ROLES", "ORGANISATION_ROLES_NOT_FOUND");
      return result;
    }

    public async Task<bool> UpdateOrganisationAuditList(WrapperOrganisationAuditInfo organisationAuditInfo)
    {
      return await _wrapperApiService.PutAsync<bool>(WrapperApi.Organisation, "audits", organisationAuditInfo, "ERROR_CREATING_ORGANISATION_AUDIT_LOG");
    }

  }
}
