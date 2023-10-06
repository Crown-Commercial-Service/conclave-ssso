using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.DataMigrationJobScheduler.Model
{
  public class DataMigrationFileListInfo : DataMigrationListInfo
  {
    public string FileKey { get; set; }
  }
  public class DataMigrationFileListResponse
  {
    public List<DataMigrationFileListInfo> DataMigrationList { get; set; }
  }
  public class DataMigrationStatusRequest
  {
    public string Id { get; set; }

    public DataMigrationStatus DataMigrationStatus { get; set; }

  }
}
