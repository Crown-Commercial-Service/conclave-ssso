using CcsSso.Core.DbModel.Constants;
using System;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos
{
  public class BulkUploadStatusResponse
  {
    public string Id { get; set; }

    public BulkUploadStatus BulkUploadStatus { get; set; }

    public List<KeyValuePair<string, string>> ErrorDetails { get; set; }

    public BulkUploadMigrationReportDetails BulkUploadMigrationReportDetails { get; set; }
  }

  public class BulkUploadMigrationReportDetails
  {
    public DateTime MigrationStartedTime { get; set; }

    public DateTime MigrationEndTime { get; set; }

    public int TotalUserCount { get; set; }

    public int TotalOrganisationCount { get; set; }

    public int ProcessedUserCount { get; set; }

    public int FailedUserCount { get; set; }

    public List<BulkUploadFileContentRowDetails> BulkUploadFileContentRowList { get; set; }
  }

  public class BulkUploadFileContentRowDetails
  {
    public string IdentifierId { get; set; }

    public string SchemeId { get; set; }

    public string RightToBuy { get; set; }

    public string Email { get; set; }

    public string Title { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Roles { get; set; }

    public string Status { get; set; }

    public string StatusDescription { get; set; }
  }
}
