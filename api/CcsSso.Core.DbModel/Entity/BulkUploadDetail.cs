using CcsSso.Core.DbModel.Constants;
using CcsSso.DbModel.Entity;
using System;

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

    public DateTime MigrationStartedOnUtc{ get; set; }

    public DateTime MigrationEndedOnUtc{ get; set; }

    public int TotalUserCount { get; set; }

    public int TotalOrganisationCount { get; set; }

    public int ProcessedUserCount { get; set; }

    public int FailedUserCount { get; set; }

    public string MigrationStringContent { get; set; }
  }
}
