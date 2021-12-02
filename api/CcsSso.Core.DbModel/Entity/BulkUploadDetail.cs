using CcsSso.Core.DbModel.Constants;
using CcsSso.DbModel.Entity;

namespace CcsSso.Core.DbModel.Entity
{
  public class BulkUploadDetail : BaseEntity
  {
    public int Id { get; set; }

    public string OrganisationId { get; set; }

    public string FileKey { get; set; }

    public string FileKeyId { get; set; }

    public string DocUploadId { get; set; }

    public BulkUploadStatus BulkUploadStatus { get; set; }

    public string ValidationErrorDetails { get; set; }
  }
}
