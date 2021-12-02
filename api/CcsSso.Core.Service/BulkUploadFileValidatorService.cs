using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class BulkUploadFileValidatorService : IBulkUploadFileValidatorService
  {
    private readonly IUserProfileHelperService _userProfileHelperService;
    private IReadOnlyList<string> validHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "title", "firstName", "lastName", "Role", "contactEmail", "contactMobile", "contactPhone", "contactFax", "contactSocial" };
    private IReadOnlyList<string> requiredHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "firstName", "lastName" };
    public BulkUploadFileValidatorService(IUserProfileHelperService userProfileHelperService)
    {
      _userProfileHelperService = userProfileHelperService;
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
      if (fileRows.Count() <= 2)
      {
        errorDetails.Add(new KeyValuePair<string, string>("File content error", "No upload data found in the file"));
        return errorDetails;
      }

      var headers = fileRows[1].Split(",");
      var headerValidationErrors = ValidateHeaders(headers.ToList());
      if (headerValidationErrors.Any())
      {
        errorDetails.AddRange(headerValidationErrors);
        return errorDetails;
      }

      var dataValidationErrors = ValidateRows(headers.ToList(), fileRows.Skip(2).ToList());
      errorDetails.AddRange(dataValidationErrors);
      return errorDetails;
    }

    private string[] GetFileRows(string fileContentString)
    {
      var fileRows = fileContentString.Split("\r\n");
      return fileRows.Where(r => !string.IsNullOrWhiteSpace(r)).ToArray();
    }

    private List<KeyValuePair<string, string>> ValidateHeaders(List<string> fileHeaders)
    {
      var errorDetails = new List<KeyValuePair<string, string>>();

      foreach (var validHeader in validHeaders)
      {
        if (!fileHeaders.Any(h => h == validHeader))
        {
          errorDetails.Add(new KeyValuePair<string, string>("Header not available", $"Header '{validHeader}' not found"));
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

      foreach (var row in rows.Select((data, i) => new { i, data }))
      {
        var fileRowNumber = row.i + 3;
        var rowDataColumns = row.data.Split(",");
        foreach (var requiredHeader in requiredHeaders)
        {
          var actualHeaderIndex = fileHeaders.FindIndex(h => h == requiredHeader);
          if (string.IsNullOrWhiteSpace(rowDataColumns[actualHeaderIndex]))
          {
            errorDetails.Add(new KeyValuePair<string, string>("Missing required value", $"'{fileHeaders[actualHeaderIndex]}' is missing in row {fileRowNumber}"));
          }
        }

        var emailHeaderIndex = fileHeaders.FindIndex(h => h == "email");
        if (!string.IsNullOrWhiteSpace(rowDataColumns[emailHeaderIndex]) && _userProfileHelperService.IsInvalidUserName(rowDataColumns[emailHeaderIndex]))
        {
          errorDetails.Add(new KeyValuePair<string, string>("Invalid email value", $"Invalid email in row {fileRowNumber}"));
        }
      }

      return errorDetails;
    }
  }
}
