using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Dtos.External;


namespace CcsSso.Core.DataMigrationJobScheduler.Model
{
  public class DataMigrationFileListInfo : DataMigrationListInfo
  {
    public string FileKey { get; set; }
  }

  public class DataMigrationFileListResponse
  {
    public List<DataMigrationFileListInfo> DataMigrationList { get; set; }
    public int filesCount { get; set; }
  }

  public class DataMigrationStatusRequest
  {
    public string Id { get; set; }

    public DataMigrationStatus DataMigrationStatus { get; set; }
  }
}
