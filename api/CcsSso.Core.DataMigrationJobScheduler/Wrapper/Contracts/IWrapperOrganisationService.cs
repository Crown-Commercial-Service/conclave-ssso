using CcsSso.Core.DataMigrationJobScheduler.Model;

namespace CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts
{
    public interface IWrapperOrganisationService
    {
      Task<DataMigrationFileListResponse> GetDataMigrationFilesList(); 
      Task UpdateDataMigrationFileStatus(DataMigrationStatusRequest dataMigrationStatusRequest);
    }
}