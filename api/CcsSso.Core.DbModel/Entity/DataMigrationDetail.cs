using CcsSso.Core.DbModel.Constants;
using CcsSso.DbModel.Entity;
using System;

namespace CcsSso.Core.DbModel.Entity
{
  public class DataMigrationDetail : BaseEntity
  {
    public int Id { get; set; }

    public string FileName { get; set; }

    public string FileKey { get; set; }

    public string FileKeyId { get; set; }

    public string DocUploadId { get; set; }

    public DataMigrationStatus DataMigrationStatus { get; set; }

    public string ValidationErrorDetails { get; set; }

    public DateTime MigrationStartedOnUtc { get; set; }

    public DateTime MigrationEndedOnUtc{ get; set; }

    public string MigrationStringContent { get; set; }

  }
}
