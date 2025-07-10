using CcsSso.Core.DbModel.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class DataMigrationListInfo
  {
    public string Id { get; set; }

    public string FileName { get; set; }

    public DateTime DateOfUpload { get; set; }

    public string Name { get; set; }

    public DataMigrationStatus Status { get; set; }

    [JsonIgnore]
    public int CreatedUserId { get; set; }

  }
}
