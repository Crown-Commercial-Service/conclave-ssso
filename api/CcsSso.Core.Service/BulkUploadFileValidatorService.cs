using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class BulkUploadFileValidatorService : IBulkUploadFileValidatorService
  {
    private readonly IUserProfileHelperService _userProfileHelperService;
    private IReadOnlyList<string> validHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "title", "firstName", "lastName", "Role", "contactEmail", "contactMobile", "contactPhone", "contactFax", "contactSocial" };
    private IReadOnlyList<string> requiredHeaders = new List<string> { "identifier-id", "scheme-id", "rightToBuy", "email", "firstName", "lastName", "Role" };
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

      var headers = fileRows[1].Split(',').Select(c => c).ToList();
      var headerValidationErrors = ValidateHeaders(headers);
      if (headerValidationErrors.Any())
      {
        errorDetails.AddRange(headerValidationErrors);
        return errorDetails;
      }

      var dataValidationErrors = ValidateRows(headers, fileRows.Skip(2).ToList());
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
        var headerCount = fileHeaders.Count(h => h == validHeader);
        if (headerCount == 0)
        {
          errorDetails.Add(new KeyValuePair<string, string>("Header not available", $"Header '{validHeader}' not found"));
        }
        else if(headerCount > 1)
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

      foreach (var row in rows.Select((data, i) => new { i, data }))
      {
        var fileRowNumber = row.i + 3;
        Regex regx = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        var rowDataColumns = regx.Split(row.data);
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
