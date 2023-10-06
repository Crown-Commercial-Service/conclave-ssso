using CcsSso.Core.DataMigrationJobScheduler.Constants;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts;
using CcsSso.Shared.Cache.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
      return await _wrapperApiService.GetAsync<DataMigrationFileListResponse>(WrapperApi.Organisation, $"migrations/files/processed", "ERROR_RETRIEVING_DM_FILES",false);
    }

    public async Task UpdateDataMigrationFileStatus(DataMigrationStatusRequest dataMigrationStatusRequest)
    {
      await _wrapperApiService.PutAsync(WrapperApi.Organisation, $"migrations/status",dataMigrationStatusRequest, "ERROR_UPDATING_DM_FILE_STATUS");
    }
  }
}
