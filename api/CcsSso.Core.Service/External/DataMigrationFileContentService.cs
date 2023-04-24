using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CcsSso.Service.External
{
  public class DataMigrationFileContentService : IDataMigrationFileContentService
  {
    private readonly IUserProfileHelperService _userProfileHelperService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private IReadOnlyList<string> validHeaders = new List<string> { "IdentifierId", "SchemeId", "OrganisationType", "EmailAddress", "DomainName", "Title", "FirstName", "LastName", "OrganisationRoles", "UserRoles", "ContactEmail", "ContactMobile", "ContactPhone", "ContactFax", "ContactSocial" };
    private IReadOnlyList<string> requiredHeaders = new List<string> { "IdentifierId", "SchemeId", "OrganisationType", "EmailAddress", "DomainName", "Title", "FirstName", "LastName", "OrganisationRoles", "UserRoles", "ContactEmail", "ContactMobile", "ContactPhone", "ContactFax", "ContactSocial" };    
    private const int migrationFileHeaderCount = 15;
    private const int headerTitleRowCount = 2;
    public DataMigrationFileContentService(IUserProfileHelperService userProfileHelperService, ApplicationConfigurationInfo applicationConfigurationInfo)
    {
      _userProfileHelperService = userProfileHelperService;
      _applicationConfigurationInfo = applicationConfigurationInfo;
    }

    public List<KeyValuePair<string, string>> ValidateUploadedFile(string fileKey, string fileContentString)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();

      if (string.IsNullOrWhiteSpace(fileContentString))
      {
        errorDetails.Add(new KeyValuePair<string, string>("File content error", "No file content"));
        return errorDetails;
      }

      var fileRows = GetFileRows(fileContentString);
      if (fileRows.Count() <= headerTitleRowCount)
      {
        errorDetails.Add(new KeyValuePair<string, string>("File content error", "No upload data found in the file"));
        return errorDetails;
      }

      var headers = GetFileHeaders(fileRows);
      var headerValidationErrors = ValidateHeaders(headers);
      if (headerValidationErrors.Any())
      {
        errorDetails.AddRange(headerValidationErrors);
        return errorDetails;
      }

      var dataValidationErrors = ValidateRows(headers, fileRows.Skip(headerTitleRowCount).ToList());
      errorDetails.AddRange(dataValidationErrors);
      return errorDetails;
    }

    public DataMigrationResult CheckMigrationStatus(string fileContentString)
    {
      var fileRows = GetFileRows(fileContentString);
      var fileHeaders = GetFileHeaders(fileRows);
      bool isCompleted = true;
      List<string> organisationIdentifiers = new();
      List<string> users = new();
      List<string> failedUsers = new();
      List<string> succeededUsers = new();

      var statusHeaderIndex = fileHeaders.FindIndex(h => h == "Status");
      var organisationHeaderIndex = fileHeaders.FindIndex(h => h == "identifier-id");
      var emailHeaderIndex = fileHeaders.FindIndex(h => h == "email");

      foreach (var row in fileRows.Skip(headerTitleRowCount).ToList())
      {
        var rowDataColumns = GetRowColumnData(row);

        if (rowDataColumns.Count() != migrationFileHeaderCount || string.IsNullOrWhiteSpace(rowDataColumns[statusHeaderIndex]))
        {
          isCompleted = false;
          break;
        }
        else
        {
          organisationIdentifiers.Add(rowDataColumns[organisationHeaderIndex]);
          users.Add(rowDataColumns[emailHeaderIndex]);
          if (rowDataColumns[statusHeaderIndex] == "Success")
          {
            succeededUsers.Add(rowDataColumns[emailHeaderIndex]);
          }
          else
          {
            failedUsers.Add(rowDataColumns[emailHeaderIndex]);
          }
        }
      }

      var totalOrganisationCount = organisationIdentifiers.Distinct().Count();
      var totalUserCount = users.Distinct().Count();
      var totalProceedUserCount = succeededUsers.Distinct().Count();
      var failedUserCount = failedUsers.Distinct().Count();

      return new DataMigrationResult
      {
        IsCompleted = isCompleted,
        TotalOrganisationCount = totalOrganisationCount,
        TotalUserCount = totalUserCount,
        FailedUserCount = failedUserCount,
        ProceededUserCount = totalProceedUserCount
      };
    }

    public List<DataMigrationFileContentRowDetails> GetFileContentObject(string fileContentString)
    {
      List<DataMigrationFileContentRowDetails> dataMigrationFileContentRowDetails = new();

      var fileRows = GetFileRows(fileContentString);

      foreach (var row in fileRows.Skip(headerTitleRowCount).ToList())
      {
        var rowDataColumns = GetRowColumnData(row);
        if (rowDataColumns.Count() == migrationFileHeaderCount)
        {
          var dataMigrationFileContentRowDetail = new DataMigrationFileContentRowDetails
          {
            IdentifierId = rowDataColumns[0],
            SchemeId = rowDataColumns[1],
            OrganisationType = rowDataColumns[2],
            EmailAddress = rowDataColumns[3],
            DomainName = rowDataColumns[4],
            Title = rowDataColumns[5],
            FirstName = rowDataColumns[6],
            LastName = rowDataColumns[7],
            OrganisationRoles = rowDataColumns[8],
            UserRoles = rowDataColumns[9],
            ContactEmail = rowDataColumns[10],
            ContactMobile = rowDataColumns[11],
            ContactPhone = rowDataColumns[12],
            ContactFax = rowDataColumns[13],
            ContactSocial = rowDataColumns[14],
          };
          dataMigrationFileContentRowDetails.Add(dataMigrationFileContentRowDetail);
        }
      }

      return dataMigrationFileContentRowDetails;
    }

    private string[] GetFileRows(string fileContentString)
    {
      var fileRows = fileContentString.Split("\r\n");
      return fileRows.Where(r => !string.IsNullOrWhiteSpace(r)).ToArray();
    }

    private List<string> GetFileHeaders(string[] fileRows)
    {
      var headers = fileRows[0].Split(',').Select(c => c).ToList();
      return headers;
    }

    private string[] GetRowColumnData(string row)
    {
      Regex regx = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
      var rowDataColumns = regx.Split(row);
      return rowDataColumns;
    }

    private List<KeyValuePair<string, string>> ValidateHeaders(List<string> fileHeaders)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();

      foreach (var validHeader in validHeaders)
      {
        var headerCount = fileHeaders.Count(h => h == validHeader);
        if (headerCount == 0)
        {
          errorDetails.Add(new KeyValuePair<string, string>("Header not available", $"Header '{validHeader}' not found"));
        }
        else if (headerCount > 1)
        {
          errorDetails.Add(new KeyValuePair<string, string>("Duplicate headers found", $"Header '{validHeader}' is duplicated"));
        }
      }
      return errorDetails;

    }

    /// <summary>
    /// Validate the row data
    /// </summary>
    /// <param name="fileHeaders"></param>
    /// <param name="rows"> Exclude the Header rows (total 2 rows according to the template)</param>
    /// <returns></returns>
    private List<KeyValuePair<string, string>> ValidateRows(List<string> fileHeaders, List<string> rows)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();

      //if (rows.Count > _applicationConfigurationInfo.DataMigrationMaxUserCount)
      //{
      //  errorDetails.Add(new KeyValuePair<string, string>("Exceeds max number of users", $"Number of users ({rows.Count}) in the csv file exceeds max number of users ({_applicationConfigurationInfo.DataMigrationMaxUserCount}) allowed. Reduce number of users in csv file and try again."));
      //  return errorDetails;
      //}

      foreach (var row in rows.Select((data, i) => new { i, data }))
      {
        var fileRowNumber = row.i + 3;
        var rowDataColumns = GetRowColumnData(row.data);
        foreach (var requiredHeader in requiredHeaders)
        {
          var actualHeaderIndex = fileHeaders.FindIndex(h => h == requiredHeader);
          if (string.IsNullOrWhiteSpace(rowDataColumns[actualHeaderIndex]))
          {
            errorDetails.Add(new KeyValuePair<string, string>("Missing required value", $"'{fileHeaders[actualHeaderIndex]}' is missing in row {fileRowNumber}"));
          }
        }

        //var emailHeaderIndex = fileHeaders.FindIndex(h => h == "email");
        //if (!string.IsNullOrWhiteSpace(rowDataColumns[emailHeaderIndex]) && !UtilityHelper.IsEmailFormatValid(rowDataColumns[emailHeaderIndex]))
        //{
        //  errorDetails.Add(new KeyValuePair<string, string>("Invalid email value", $"Invalid email in row {fileRowNumber}"));
        //}
        //else if (!string.IsNullOrWhiteSpace(rowDataColumns[emailHeaderIndex]) && !UtilityHelper.IsEmailLengthValid(rowDataColumns[emailHeaderIndex]))
        //{
        //  errorDetails.Add(new KeyValuePair<string, string>("Invalid email length", $"Email length exceeded in row {fileRowNumber}"));
        //}

        ////boolean field validation
        //var rightToBuyHeaderIndex = fileHeaders.FindIndex(h => h == "rightToBuy");
        //if (!string.IsNullOrWhiteSpace(rowDataColumns[rightToBuyHeaderIndex]) && !Boolean.TryParse(rowDataColumns[rightToBuyHeaderIndex], out bool parsedValue))
        //{
        //  errorDetails.Add(new KeyValuePair<string, string>("Invalid rightToBuy value", $"Invalid value for rightToBuy in row {fileRowNumber}"));
        //}
      }

      return errorDetails;
    }
  }
}
