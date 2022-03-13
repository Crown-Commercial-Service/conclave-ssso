using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CcsSso.Core.Service
{
  public class BulkUploadFileContentService : IBulkUploadFileContentService
  {
    private readonly IUserProfileHelperService _userProfileHelperService;
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private IReadOnlyList<string> validHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "title", "firstName", "lastName", "Role", "contactEmail", "contactMobile", "contactPhone", "contactFax", "contactSocial" };
    private IReadOnlyList<string> requiredHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "firstName", "lastName", "Role" };
    private IReadOnlyList<string> reportHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "title", "firstName", "lastName", "Role", "Status", "Status description" };
    private const int migrationFileHeaderCount = 15;
    private const int headerTitleRowCount = 2;
    public BulkUploadFileContentService(IUserProfileHelperService userProfileHelperService, ApplicationConfigurationInfo applicationConfigurationInfo)
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

    public BulkUploadMigrationResult CheckMigrationStatus(string fileContentString)
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

      return new BulkUploadMigrationResult
      {
        IsCompleted = isCompleted,
        TotalOrganisationCount = totalOrganisationCount,
        TotalUserCount = totalUserCount,
        FailedUserCount = failedUserCount,
        ProceededUserCount = totalProceedUserCount
      };
    }

    public List<BulkUploadFileContentRowDetails> GetFileContentObject(string fileContentString)
    {
      List<BulkUploadFileContentRowDetails> bulkUploadFileContentRowDetails = new();

      var fileRows = GetFileRows(fileContentString);

      foreach (var row in fileRows.Skip(headerTitleRowCount).ToList())
      {
        var rowDataColumns = GetRowColumnData(row);
        if (rowDataColumns.Count() == migrationFileHeaderCount)
        {
          var bulkUploadFileContentRowDetail = new BulkUploadFileContentRowDetails
          {
            IdentifierId = rowDataColumns[0],
            SchemeId = rowDataColumns[1],
            RightToBuy = rowDataColumns[2],
            Email = rowDataColumns[3],
            Title = rowDataColumns[4],
            FirstName = rowDataColumns[5],
            LastName = rowDataColumns[6],
            Roles = rowDataColumns[7],
            Status = rowDataColumns[13],
            StatusDescription = rowDataColumns[14]
          };
          bulkUploadFileContentRowDetails.Add(bulkUploadFileContentRowDetail);
        }
      }

      return bulkUploadFileContentRowDetails;
    }

    private string[] GetFileRows(string fileContentString)
    {
      var fileRows = fileContentString.Split("\r\n");
      return fileRows.Where(r => !string.IsNullOrWhiteSpace(r)).ToArray();
    }

    private List<string> GetFileHeaders(string[] fileRows)
    {
      var headers = fileRows[1].Split(',').Select(c => c).ToList();
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

      if (rows.Count > _applicationConfigurationInfo.BulkUploadMaxUserCount)
      {
        errorDetails.Add(new KeyValuePair<string, string>("Exceeds max number of users", $"Number of users ({rows.Count}) in the csv file exceeds max number of users ({_applicationConfigurationInfo.BulkUploadMaxUserCount}) allowed. Reduce number of users in csv file and try again."));
        return errorDetails;
      }

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

        var emailHeaderIndex = fileHeaders.FindIndex(h => h == "email");
        if (!string.IsNullOrWhiteSpace(rowDataColumns[emailHeaderIndex]) && !UtilityHelper.IsEmailFormatValid(rowDataColumns[emailHeaderIndex]))
        {
          errorDetails.Add(new KeyValuePair<string, string>("Invalid email value", $"Invalid email in row {fileRowNumber}"));
        }
        else if (!string.IsNullOrWhiteSpace(rowDataColumns[emailHeaderIndex]) && !UtilityHelper.IsEmailLengthValid(rowDataColumns[emailHeaderIndex]))
        {
          errorDetails.Add(new KeyValuePair<string, string>("Invalid email length", $"Email length exceeded in row {fileRowNumber}"));
        }

        //boolean field validation
        var rightToBuyHeaderIndex = fileHeaders.FindIndex(h => h == "rightToBuy");
        if (!string.IsNullOrWhiteSpace(rowDataColumns[rightToBuyHeaderIndex]) && !Boolean.TryParse(rowDataColumns[rightToBuyHeaderIndex], out bool parsedValue))
        {
          errorDetails.Add(new KeyValuePair<string, string>("Invalid rightToBuy value", $"Invalid value for rightToBuy in row {fileRowNumber}"));
        }
      }

      return errorDetails;
    }
  }
}
