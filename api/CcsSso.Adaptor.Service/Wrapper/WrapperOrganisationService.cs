using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Wrapper
{
  public class WrapperOrganisationService : IWrapperOrganisationService
  {
    private readonly IWrapperApiService _wrapperApiService;
    private readonly IRemoteCacheService _remoteCacheService;
    private readonly AppSetting _appSetting;
    public WrapperOrganisationService(IWrapperApiService wrapperApiService, IRemoteCacheService remoteCacheService,
      AppSetting appSetting)
    {
      _wrapperApiService = wrapperApiService;
      _remoteCacheService = remoteCacheService;
      _appSetting = appSetting;
    }

    public async Task<WrapperOrganisationResponse> GetOrganisationAsync(string organisationId)
    {
      var result = await _wrapperApiService.GetAsync<WrapperOrganisationResponse>(WrapperApi.Organisation, $"{organisationId}", $"{CacheKeyConstant.Organisation}-{organisationId}", "ERROR_RETRIEVING_ORGANISATION");
      return result;
    }

    public async Task<List<WrapperUserListInfo>> GetOrganisationUsersAsync(string organisationId)
    {
      if (_appSetting.RedisCacheSettings.IsEnabled)
      {
        var result = await _remoteCacheService.GetValueAsync<List<WrapperUserListInfo>>($"{CacheKeyConstant.OrganisationUsers}-{organisationId}");
        if (result != null)
        {
          return result;
        }
      }

      List<WrapperUserListInfo> orgUsers = new();
      int currentPage = 0;
      int pageCount;
      do
      {
        currentPage++;
        var userListPagedInfo = await GetOrganisationUserPagedResultAsync(organisationId, _appSetting.OrganisationUserRequestPageSize, currentPage);
        orgUsers.AddRange(userListPagedInfo.UserList);
        pageCount = userListPagedInfo.PageCount;
      } while (currentPage < pageCount);

      if (_appSetting.RedisCacheSettings.IsEnabled)
      {
        await _remoteCacheService.SetValueAsync<List<WrapperUserListInfo>>($"{CacheKeyConstant.OrganisationUsers}-{organisationId}", orgUsers,
          new TimeSpan(0, _appSetting.RedisCacheSettings.CacheExpirationInMinutes, 0));
      }
      return orgUsers;
    }

    public async Task<string> UpdateOrganisationAsync(string organisationId, WrapperOrganisationRequest wrapperOrganisationRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.Organisation, $"{organisationId}", wrapperOrganisationRequest, "ERROR_UPDATING_ORGANISATION");      return organisationId;
    }

    private async Task<WrapperUserListPaginationResponse> GetOrganisationUserPagedResultAsync(string organisationId, int pageSize, int currentPage)
    {
      var result = await _wrapperApiService.GetAsync<WrapperUserListPaginationResponse>(WrapperApi.Organisation, $"{organisationId}/user?pageSize={pageSize}&currentPage={currentPage}",
        string.Empty, "ERROR_RETRIEVING_ORGANISATION_USERS", false);
      return result;
    }
  }
}
