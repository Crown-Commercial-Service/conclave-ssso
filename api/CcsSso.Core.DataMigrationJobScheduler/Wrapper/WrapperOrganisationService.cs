using CcsSso.Core.DataMigrationJobScheduler.Constants;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts;

namespace CcsSso.Core.DataMigrationJobScheduler.Wrapper
{
  public class WrapperOrganisationService : IWrapperOrganisationService
  {
    private readonly IWrapperApiService _wrapperApiService;

    public WrapperOrganisationService(IWrapperApiService wrapperApiService)
    {
      _wrapperApiService = wrapperApiService;
    }

    public async Task<DataMigrationFileListResponse> GetDataMigrationFilesList()
    {
      return await _wrapperApiService.GetAsync<DataMigrationFileListResponse>(WrapperApi.Organisation, $"migrations/files/validated?PageSize=10&CurrentPage=1", "ERROR_RETRIEVING_DM_FILES",false);
    }

    public async Task UpdateDataMigrationFileStatus(DataMigrationStatusRequest dataMigrationStatusRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.Organisation, $"migrations/status",dataMigrationStatusRequest, "ERROR_UPDATING_DM_FILE_STATUS");
    }
  }
}
