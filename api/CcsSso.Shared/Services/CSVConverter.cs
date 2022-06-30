using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using CcsSso.Shared.Domain.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CcsSso.Shared.Services
{
  public class CSVConverter : ICSVConverter
  {

    public byte[] ConvertToCSV(dynamic inputModel, string filetype)
    {
      string filepath = String.Empty;
      // return Array.Empty<byte>();
      try
      {
        if (filetype == "organisation")
        {
          var dataTable = OrganisationToTable(inputModel);
          return GetBytesFromDatatable(dataTable);
        }
        return Array.Empty<Byte>();

      }
      catch (Exception ex)
      {
        throw ex;
      }
    }


    private byte[] GetBytesFromDatatable(DataTable table)
    {
      byte[] data = null;
      using (MemoryStream stream = new MemoryStream())
      {
        BinaryFormatter bf = new BinaryFormatter();
        table.RemotingFormat = SerializationFormat.Binary;
        bf.Serialize(stream, table);
        data = stream.ToArray();
      }
      File.WriteAllBytes(@"E:\BRICK\test.csv", data);
      return data;
    }


    private DataTable OrganisationToTable(List<OrganisationProfileResponseInfo> orgProfileList)
    {
      DataTable dt = new DataTable();
      try
      {
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

        foreach (var item in orgProfileList)
        {
          string addtionalIdentifiers = (item.AdditionalIdentifiers != null && item.AdditionalIdentifiers.Any()) ? JsonConvert.SerializeObject(item.AdditionalIdentifiers) : "";
          dt.Rows.Add(
             item.Identifier.Id,
             item.Identifier.LegalName,
             item.Identifier.Uri,
             item.Identifier.Scheme,
             addtionalIdentifiers,
             item.Address.StreetAddress,
             item.Address.Locality,
             item.Address.Region,
             item.Address.PostalCode,
             item.Address.CountryCode,
             item.Address.CountryName,
             Convert.ToString(item.Detail.OrganisationId),
             item.Detail.CreationDate,
             item.Detail.BusinessType,
             item.Detail.SupplierBuyerType,
             item.Detail.IsSme,
             item.Detail.IsVcse,
             item.Detail.RightToBuy,
             item.Detail.IsActive);
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }

      return dt;
    }
  }

  public class OrganizationRoot
  {
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