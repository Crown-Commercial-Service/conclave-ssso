using CcsSso.Core.DbModel.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class DataMigrationStatusResponse
  {
    public string Id { get; set; }

    public DataMigrationStatus DataMigrationStatus { get; set; }

    public List<KeyValuePair<string, string>> ErrorDetails { get; set; }

    public DataMigrationMigrationReportDetails DataMigrationMigrationReportDetails { get; set; }
  }

  public class DataMigrationMigrationReportDetails
  {
    public DateTime MigrationStartedTime { get; set; }

    public DateTime MigrationEndTime { get; set; }

    public List<DataMigrationFileContentRowDetails> DataMigrationFileContentRowList { get; set; }
  }

  public class DataMigrationFileContentRowDetails
  {
    public string IdentifierId { get; set; }

    public string SchemeId { get; set; }

    public string OrganisationType { get; set; }

    public string EmailAddress { get; set; }

    public string DomainName { get; set; }

    public string Title { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string OrganisationRoles { get; set; }

    public string UserRoles { get; set; }

    public string ContactEmail { get; set; }

    public string ContactMobile { get; set; }

    public string ContactPhone { get; set; }

    public string ContactFax { get; set; }

    public string ContactSocial { get; set; }
  }

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

  public class DataMigrationListResponse : PaginationInfo
  {
    public List<DataMigrationListInfo> DataMigrationList { get; set; }
  }
}
