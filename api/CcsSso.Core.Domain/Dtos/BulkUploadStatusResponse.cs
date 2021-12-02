using CcsSso.Core.DbModel.Constants;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos
{
  public class BulkUploadStatusResponse
  {
    public string Id { get; set; }

    public BulkUploadStatus BulkUploadStatus { get; set; }

    public List<KeyValuePair<string, string>> ErrorDetails { get; set; }
  }
}
