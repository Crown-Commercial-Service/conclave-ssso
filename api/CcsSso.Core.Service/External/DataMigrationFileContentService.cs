using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CcsSso.Service.External
{
  public class DataMigrationFileContentService : IDataMigrationFileContentService
  {
    private readonly IUserProfileService _userProfileService;
    private readonly ICiiService _ciiService;
    private IReadOnlyList<string> validHeaders = new List<string> { "IdentifierId", "SchemeId", "OrganisationType", "EmailAddress", "DomainName", "Title", "FirstName", "LastName", "OrganisationRoles", "UserRoles", "ContactEmail", "ContactMobile", "ContactPhone", "ContactFax", "ContactSocial" };
    private IReadOnlyList<string> requiredHeaders = new List<string> { "IdentifierId", "SchemeId", "OrganisationType", "EmailAddress", "FirstName", "LastName", "UserRoles"   };    
    private const int migrationFileHeaderCount = 15;
    private const int headerTitleRowCount = 2;
    public DataMigrationFileContentService(IUserProfileService userProfileService, ICiiService ciiService)
    {
      _userProfileService = userProfileService;
      _ciiService = ciiService;
    }

    public async Task<List<KeyValuePair<string, string>>> ValidateUploadedFile(string fileKey, string fileContentString)
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

      var dataValidationErrors = await ValidateRows(headers, fileRows.Skip(headerTitleRowCount).ToList());
      errorDetails.AddRange(dataValidationErrors);
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

    private void GetRowContentObject(List<DataMigrationFileContentRowDetails> dataMigrationFileContentRowDetails, string row)
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
    private async Task<List<KeyValuePair<string, string>>> ValidateRows(List<string> fileHeaders, List<string> rows)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();

      var users = await _userProfileService.GetUsersAsync();
      var scheme = await _ciiService.GetSchemesAsync();

      foreach (var row in rows.Select((data, i) => new { i, data }))
      {
        var fileRowNumber = row.i + 3;
        var rowDataColumns = GetRowColumnData(row.data);

        ValidateForRequired(fileHeaders, errorDetails, fileRowNumber, rowDataColumns);

        var schemeIdHeaderIndex = fileHeaders.FindIndex(h => h == "SchemeId");
        var schemeId = rowDataColumns[schemeIdHeaderIndex];
        var isValidSchemeId = ValidateSchemeId(errorDetails, scheme, fileRowNumber, schemeId);

        var identifierIdHeaderIndex = fileHeaders.FindIndex(h => h == "IdentifierId");
        var identifierId = rowDataColumns[identifierIdHeaderIndex];
        if (!string.IsNullOrWhiteSpace(identifierId) && isValidSchemeId)
        {
          await ValidateIdentifierId(errorDetails, fileRowNumber, schemeId, identifierId);
        }

        var emailHeaderIndex = fileHeaders.FindIndex(h => h == "EmailAddress");
        var email = rowDataColumns[emailHeaderIndex];
        ValidateEmailAddress(errorDetails, users, fileRowNumber, email);

        ValidateNameOfUser(fileHeaders, errorDetails, fileRowNumber, rowDataColumns);
      }

      return errorDetails;
    }

    private static void ValidateNameOfUser(List<string> fileHeaders, List<KeyValuePair<string, string>> errorDetails, int fileRowNumber, string[] rowDataColumns)
    {
      var firstNameHeaderIndex = fileHeaders.FindIndex(h => h == "FirstName");
      var firstName = rowDataColumns[firstNameHeaderIndex];
      ValidateFirstName(errorDetails, fileRowNumber, firstName);

      var lastNameHeaderIndex = fileHeaders.FindIndex(h => h == "LastName");
      var lastName = rowDataColumns[lastNameHeaderIndex];
      ValidateLastName(errorDetails, fileRowNumber, lastName);
    }

    private static bool ValidateSchemeId(List<KeyValuePair<string, string>> errorDetails, Dtos.Domain.Models.CiiSchemeDto[] scheme, int fileRowNumber, string schemeId)
    {
      if (!string.IsNullOrWhiteSpace(schemeId) && !scheme.Any(x => x.scheme == schemeId))
      {
        errorDetails.Add(new KeyValuePair<string, string>("The scheme id does not match possible formats", $"Row {fileRowNumber}"));
        return false;
      }
      return true;
    }

    private void ValidateForRequired(List<string> fileHeaders, List<KeyValuePair<string, string>> errorDetails, int fileRowNumber, string[] rowDataColumns)
    {
      foreach (var requiredHeader in requiredHeaders)
      {
        var actualHeaderIndex = fileHeaders.FindIndex(h => h == requiredHeader);
        if (string.IsNullOrWhiteSpace(rowDataColumns[actualHeaderIndex]))
        {
          errorDetails.Add(new KeyValuePair<string, string>($"'{fileHeaders[actualHeaderIndex]}' must have an entry", $"Row {fileRowNumber}"));
        }
      }
    }

    private async Task ValidateIdentifierId(List<KeyValuePair<string, string>> errorDetails, int fileRowNumber, string schemeId, string identifierId)
    {
      var isValidIdentifierId = false;
      try
      {
        var identifierDetails = await _ciiService.GetIdentifierDetailsAsync(schemeId, identifierId);
        if (identifierDetails != null)
        {
          isValidIdentifierId = true;
        }
      }
      catch (Exception)
      {
      }
      if (!isValidIdentifierId)
      {
        errorDetails.Add(new KeyValuePair<string, string>("The organisation identifier id does not match possible formats", $"Row {fileRowNumber}"));
      }
    }

    private static void ValidateLastName(List<KeyValuePair<string, string>> errorDetails, int fileRowNumber, string lastName)
    {
      if (!string.IsNullOrWhiteSpace(lastName) && !UtilityHelper.IsLastNameFormatValid(lastName))
      {
        errorDetails.Add(new KeyValuePair<string, string>("You have input last name in an invalid format", $"Row {fileRowNumber}"));
      }
    }

    private static void ValidateFirstName(List<KeyValuePair<string, string>> errorDetails, int fileRowNumber, string firstName)
    {
      if (!string.IsNullOrWhiteSpace(firstName) && !UtilityHelper.IsFirstNameFormatValid(firstName))
      {
        errorDetails.Add(new KeyValuePair<string, string>("You have input first name in an invalid format", $"Row {fileRowNumber}"));
      }
    }

    private static void ValidateEmailAddress(List<KeyValuePair<string, string>> errorDetails, List<User> users, int fileRowNumber, string email)
    {
      if (!string.IsNullOrWhiteSpace(email))
      {
        if (!UtilityHelper.IsEmailFormatValid(email))
        {
          errorDetails.Add(new KeyValuePair<string, string>("You have input an invalid email address", $"Row {fileRowNumber}"));
        }
        else if (!UtilityHelper.IsEmailLengthValid(email))
        {
          errorDetails.Add(new KeyValuePair<string, string>("You have input an invalid email length", $"Row {fileRowNumber}"));
        }
        else if (users.Any(x => x.UserName == email))
        {
          errorDetails.Add(new KeyValuePair<string, string>("This email address already exists withing PPG.", $"Row {fileRowNumber}"));
        }
      }
    }
  }
}
