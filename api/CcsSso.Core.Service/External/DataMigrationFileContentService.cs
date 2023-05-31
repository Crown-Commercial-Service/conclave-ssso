using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CcsSso.Service.External
{
  public partial class DataMigrationFileContentService : IDataMigrationFileContentService
  {
    private readonly IUserProfileService _userProfileService;
    private readonly ICiiService _ciiService;
    private IReadOnlyList<string> validHeaders = new List<string> { "IdentifierId", "SchemeId", "OrganisationType", "EmailAddress", "DomainName", "Title", "FirstName", "LastName", "OrganisationRoles", "UserRoles", "ContactEmail", "ContactMobile", "ContactPhone", "ContactFax", "ContactSocial" };
    private IReadOnlyList<string> requiredHeaders = new List<string> { "OrganisationType", "EmailAddress", "FirstName", "LastName", "UserRoles" };
    private const int migrationFileHeaderCount = 15;
    private const int headerTitleRowCount = 2;
    private List<User> users = null;
    private CiiSchemeDto[] schemes = null;

    public DataMigrationFileContentService(IUserProfileService userProfileService, ICiiService ciiService)
    {
      _userProfileService = userProfileService;
      _ciiService = ciiService;
    }

    public async Task<List<KeyValuePair<string, string>>> ValidateUploadedFile(string fileKey, string fileContentString)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();

      ValidateFileContentString(fileContentString, errorDetails);

      var fileRows = GetFileRows(fileContentString);

      ValidateHeaderTitleRowCount(errorDetails, fileRows);

      var headers = GetFileHeaders(fileRows);

      ValidateFileTemplate(errorDetails, fileRows, headers);

      if (!errorDetails.Any())
      {
        var dataValidationErrors = await ValidateRows(headers, fileRows.Skip(headerTitleRowCount).ToList());
        errorDetails.AddRange(dataValidationErrors);
      }

      return errorDetails;
    }
        
    public List<DataMigrationFileContentRowDetails> GetFileContentObject(string fileContentString)
    {
      List<DataMigrationFileContentRowDetails> dataMigrationFileContentRowDetails = new();

      var fileRows = GetFileRows(fileContentString);
      foreach (var row in fileRows.Skip(headerTitleRowCount).ToList())
      {
        GetRowContentObject(dataMigrationFileContentRowDetails, row);
      }

      return dataMigrationFileContentRowDetails;
    }
        
  }
}
