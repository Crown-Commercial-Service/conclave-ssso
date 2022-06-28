using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CcsSso.Shared.Services
{
  public static class JSONtoCSVConvertorService
  {
    public static void jsonStringToCSV(string jsonContent,  string type)
    {
      string filepath = String.Empty;

      try
      {
        //Datatable to CSV
        var dataTable = new DataTable();
        if (type == "organisation")
        {
          dataTable = Organisation_jsonStringToTable(jsonContent);
        }
        else if (type == "user")
        {
          dataTable = User_jsonStringToTable(jsonContent);
        }

        var lines = new List<string>();
        string[] columnNames = dataTable.Columns.Cast<DataColumn>().
                                          Select(column => column.ColumnName).
                                          ToArray();
        var header = string.Join(",", columnNames);
        lines.Add(header);
        var valueLines = dataTable.AsEnumerable()
                           .Select(row => string.Join(",", row.ItemArray));
        lines.AddRange(valueLines);

        // save CSV file
        var CurrentDate = DateTime.UtcNow; ;
        if (type == "organisation")
        {
          filepath = "D:\\Reports\\Organisations\\" + CurrentDate + "_PPG_Organisations.csv";
        }
        else if (type == "user")
        {
         //filepath = "D:\\Reports\\Users\\" + fileid + "_User.csv";
        }
        File.WriteAllLines(filepath, lines);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public static DataTable Organisation_jsonStringToTable(string jsonContent)
    {
      OrganisationJSON jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<OrganisationJSON>(jsonContent, new JsonSerializerSettings
      {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
      });
      DataTable dt = new DataTable();
      dt.Columns.Add("Identifier_Id", typeof(string));
      dt.Columns.Add("Identifier_LegalName", typeof(string));
      dt.Columns.Add("Identifier_Uri", typeof(string));
      dt.Columns.Add("Identifier_Scheme", typeof(string));
      dt.Columns.Add("AdditionalIdentifiers", typeof(string));
      dt.Columns.Add("Address_streetAddress", typeof(string));
      dt.Columns.Add("Address_locality", typeof(string));
      dt.Columns.Add("Address_region", typeof(string));
      dt.Columns.Add("Address_postalCode", typeof(string));
      dt.Columns.Add("Address_countryCode", typeof(string));
      dt.Columns.Add("Address_countryName", typeof(string));
      dt.Columns.Add("detail_organisationId", typeof(string));
      dt.Columns.Add("detail_creationDate", typeof(string));
      dt.Columns.Add("detail_businessType", typeof(string));
      dt.Columns.Add("detail_supplierBuyerType", typeof(int));
      dt.Columns.Add("detail_isSme", typeof(bool));
      dt.Columns.Add("detail_isVcse", typeof(bool));
      dt.Columns.Add("detail_rightToBuy", typeof(bool));
      dt.Columns.Add("detail_isActive", typeof(bool));
      string addtionalIdentifiers = (jsonObject.additionalIdentifiers != null && jsonObject.additionalIdentifiers.Any()) ? String.Join(",", jsonObject.additionalIdentifiers) : "";
      dt.Rows.Add(
         jsonObject.identifier.id,
         jsonObject.identifier.legalName,
         jsonObject.identifier.uri,
         jsonObject.identifier.scheme,
         addtionalIdentifiers,
         jsonObject.address.streetAddress,
         jsonObject.address.locality,
         jsonObject.address.region,
         jsonObject.address.postalCode,
         jsonObject.address.countryCode,
         jsonObject.address.countryName,
         Convert.ToString(jsonObject.detail.organisationId),
         jsonObject.detail.creationDate,
         jsonObject.detail.businessType,
         jsonObject.detail.supplierBuyerType,
         jsonObject.detail.isSme,
         jsonObject.detail.isVcse,
         jsonObject.detail.rightToBuy,
         jsonObject.detail.isActive);
      return dt;
    }

    public static DataTable User_jsonStringToTable(string jsonContent)
    {
      UserJSON jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<UserJSON>(jsonContent, new JsonSerializerSettings
      {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
      });
      DataTable dt = new DataTable();
      dt.Columns.Add("userName", typeof(string));
      dt.Columns.Add("organisationId", typeof(string));
      dt.Columns.Add("firstName", typeof(string));
      dt.Columns.Add("lastName", typeof(string));
      dt.Columns.Add("title", typeof(string));
      dt.Columns.Add("mfaEnabled", typeof(bool));
      dt.Columns.Add("password", typeof(string));
      dt.Columns.Add("accountVerified", typeof(bool));
      dt.Columns.Add("sendUserRegistrationEmail", typeof(bool));

      string groupsInfo = (jsonObject.userDetails.userGroups != null && jsonObject.userDetails.userGroups.Any()) ? String.Join(",", jsonObject.userDetails.userGroups) : "";
     // string rolePermissionInfo = (jsonObject.userDetails.rolePermissionInfo != null && jsonObject.userDetails.rolePermissionInfo.Any()) ? String.Join(",", jsonObject.userDetails.rolePermissionInfo) : "";
      string identityProviders = (jsonObject.userDetails.identityProviders != null && jsonObject.userDetails.identityProviders.Any()) ? String.Join(",", jsonObject.userDetails.identityProviders) : "";
      dt.Rows.Add(
         jsonObject.userName,
         jsonObject.organisationId,
         jsonObject.firstName,
         jsonObject.lastName,
         jsonObject.organisationId,
         jsonObject.organisationId,
         jsonObject.organisationId,
         jsonObject.organisationId,
         jsonObject.organisationId);
      return dt;
    }
  }

  public class OrganisationJSON
  {
    public Identifier identifier { get; set; }
    public List<AdditionalIdentifiers> additionalIdentifiers { get; set; }
    public OrgAddress address { get; set; }
    public OrgDetail detail { get; set; }
  }

  public class Identifier
  {
    public string id { get; set; }
    public string legalName { get; set; }
    public string uri { get; set; }
    public string scheme { get; set; }
  }

  public class AdditionalIdentifiers
  {
    public string id { get; set; }
    public string legalName { get; set; }
    public string uri { get; set; }
    public string scheme { get; set; }
  }

  public class OrgAddress
  {
    public string streetAddress { get; set; }
    public string locality { get; set; }
    public string region { get; set; }
    public string postalCode { get; set; }
    public string countryCode { get; set; }
    public string countryName { get; set; }
  }

  public class OrgDetail
  {
    public string organisationId { get; set; }
    public string creationDate { get; set; }
    public string businessType { get; set; }
    public int supplierBuyerType { get; set; }
    public bool isSme { get; set; }
    public bool isVcse { get; set; }
    public bool rightToBuy { get; set; }
    public bool isActive { get; set; }
  }

  public class UserJSON
  {
    public string userName { get; set; }
    public string organisationId { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string title { get; set; }
    public bool mfaEnabled { get; set; }
    public string password { get; set; }
    public bool accountVerified { get; set; }
    public bool sendUserRegistrationEmail { get; set; }

    public UserDetails userDetails { get; set; }
  }

  public class UserDetails
  {
    public Int64 id { get; set; }
    public bool canChangePassword { get; set; }

    public List<UserGroups> userGroups { get; set; }
   // public List<RolePermissionInfo> rolePermissionInfo { get; set; }
    public List<IdentityProviders> identityProviders { get; set; }
  }

  public class UserGroups
  {
    public Int64 groupId { get; set; }
    public string accessRole { get; set; }
    public string accessRoleName { get; set; }
    public string group { get; set; }
    public string serviceClientId { get; set; }
    public string serviceClientName { get; set; }
  }

  public class RolePermissionInfo2
  {
    public Int64 roleId { get; set; }
    public string roleName { get; set; }
    public string roleKey { get; set; }
    public string serviceClientId { get; set; }
    public string serviceClientName { get; set; }
  }

  public class IdentityProviders
  {
    public Int64 identityProviderId { get; set; }
    public string identityProvider { get; set; }
    public string identityProviderDisplayName { get; set; }
  }
}



