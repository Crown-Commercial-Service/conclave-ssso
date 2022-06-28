using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CcsSso.Shared.Services
{
  public static class JSONtoCSVConvertorService
  {
    public static void jsonStringToCSV(dynamic jsonArrayObject, string filetype)
    {
      string filepath = String.Empty;

      try
      {
        //Datatable to CSV
        var dataTable = new DataTable();
        if (filetype == "organisation")
        {
          dataTable = Organisation_jsonStringToTable(jsonArrayObject);
          //byte[] byteArr =  GetBytesFromDatatable(dataTable);
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
        var CurrentDate = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        if (filetype == "organisation")
        {
          filepath = "D:\\Reports\\Organisations\\" + CurrentDate + "_PPG_Organisations.csv";
        }
        File.WriteAllLines(filepath, lines);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public static byte[] GetBytesFromDatatable(DataTable table)
    {
      byte[] data = null;
      using (MemoryStream stream = new MemoryStream())
      {
        BinaryFormatter bf = new BinaryFormatter();
        table.RemotingFormat = SerializationFormat.Binary;
        bf.Serialize(stream, table);
        data = stream.ToArray();
      }
      return data;
    }

    public static DataTable Organisation_jsonStringToTable(dynamic jsonArrayObject)
    {
      DataTable dt = new DataTable();
      try
      {
        List<OrganisationJSON> jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OrganisationJSON>>(jsonArrayObject, new JsonSerializerSettings
        {
          MissingMemberHandling = MissingMemberHandling.Ignore,
          NullValueHandling = NullValueHandling.Ignore
        });
       
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

        foreach (var item in jsonObject)
        {
          string addtionalIdentifiers = (item.additionalIdentifiers != null && item.additionalIdentifiers.Any()) ? String.Join(",", item.additionalIdentifiers) : "";
          dt.Rows.Add(
             item.identifier.id,
             item.identifier.legalName,
             item.identifier.uri,
             item.identifier.scheme,
             addtionalIdentifiers,
             item.address.streetAddress,
             item.address.locality,
             item.address.region,
             item.address.postalCode,
             item.address.countryCode,
             item.address.countryName,
             Convert.ToString(item.detail.organisationId),
             item.detail.creationDate,
             item.detail.businessType,
             item.detail.supplierBuyerType,
             item.detail.isSme,
             item.detail.isVcse,
             item.detail.rightToBuy,
             item.detail.isActive);
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }
       
      return dt;
    }
  }

  public class OrganizationRoot {
    public List<OrganisationJSON> Organisation { get; set; }
  }
  public class OrganisationJSON
  {
    public Identifier identifier { get; set; }
    public List<AdditionalIdentifiers> additionalIdentifiers { get; set; }
    public OrgAddress address { get; set; }
    public Detail detail { get; set; }
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

  public class Detail
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
}





