using ChoETL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Xml;

namespace CcsSso.Shared.Services
{
  public static class JSONConvertorService
  {
    public static void jsonStringToCSV(string jsonContent)
    {
      string json = jsonContent;

      XmlNode xml = JsonConvert.DeserializeXmlNode("{records:{record:" + json + "}}");

      XmlDocument xmldoc = new XmlDocument(); xmldoc.LoadXml(xml.InnerXml);

      DataSet dataSet = new DataSet(); dataSet.ReadXml(new XmlNodeReader(xmldoc));
      string path = "D:\\samplecsv.csv";

      using (var writer = new StreamWriter(path))
      {
        for (int i = 0; i < dataSet.Tables.Count; i++)
        {
          writer.WriteLine(string.Join(",", dataSet.Tables[i].Columns.Cast<DataColumn>().Select(dc => dc.ColumnName)));
          foreach (DataRow row in dataSet.Tables[i].Rows)
          {
            writer.WriteLine(string.Join(",", row.ItemArray));
          }
        }
      }
    }

    public static void jsonStringToCSV_3(string jsonContent)
    {
      //used NewtonSoft json nuget package
      XmlNode xml = JsonConvert.DeserializeXmlNode("{records:{record:" + jsonContent + "}}");
      XmlDocument xmldoc = new XmlDocument();
      xmldoc.LoadXml(xml.InnerXml);
      XmlReader xmlReader = new XmlNodeReader(xml);
      DataSet dataSet = new DataSet();
      dataSet.ReadXml(xmlReader);
      var dataTable = dataSet.Tables[1];

      //Datatable to CSV
      var lines = new List<string>();
      string[] columnNames = dataTable.Columns.Cast<DataColumn>().
                                        Select(column => column.ColumnName).
                                        ToArray();
      var header = string.Join(",", columnNames);
      lines.Add(header);
      var valueLines = dataTable.AsEnumerable()
                         .Select(row => string.Join(",", row.ItemArray));
      lines.AddRange(valueLines);
      File.WriteAllLines(@"D:\Export.csv", lines);
    }

    //To convert JSON string to DataTable
    public static DataTable JsonStringToTable(string jsonContent)
    {
      DataTable dt = JsonConvert.DeserializeObject<DataTable>(jsonContent);
      return dt;
    }

    //To make CSV string
    public static void jsonStringToCSV_4(string jsonContent)
    {
      XmlNode xml = JsonConvert.DeserializeXmlNode("{records:{record:" + jsonContent + "}}");

      XmlDocument xmldoc = new XmlDocument(); xmldoc.LoadXml(xml.InnerXml);

      DataSet dataSet = new DataSet();

      dataSet.ReadXml(new XmlNodeReader(xmldoc));
      TestMyNewDataTable(dataSet);
    }

    private static void TestMyNewDataTable(DataSet dataSet)
    {
      DataTable oneDTable = new DataTable();
      bool emptyCols = false;

      // Modify the columns
      for (int i = 0; i < dataSet.Tables.Count; i++)
      {
        foreach (DataColumn item in dataSet.Tables[i].Columns)
        {
          dataSet.Tables[i].Columns.Remove(string.Empty);
          if (item != null)
          {
            emptyCols = false;
            dataSet.Tables[i].Columns[item.ColumnName].ColumnName = dataSet.Tables[i].TableName + "_" + item.ColumnName;
            dataSet.Tables[i].AcceptChanges();
          }
        }
      }

      string strFilePath = @"D:\MyTesting.csv";
      DataTable dt = new DataTable();
      StringBuilder sb = new StringBuilder();
      DataTable newdt = new DataTable();
      for (int i = 0; i < dataSet.Tables.Count; i++)
      {
        dt = dataSet.Tables[i];
        //sb = new StringBuilder();

        int count = 1;
        int totalColumns = dt.Columns.Count;
        foreach (DataColumn dr in dt.Columns)
        {
          if (dr.ColumnName != "record_Id")
          {
            sb.Append(dr.ColumnName);

            if (count != totalColumns)
            {
              sb.Append(",");
            }

          }
          count++;
        }

        sb.AppendLine();

        string value = String.Empty;
        foreach (DataRow dr in dt.Rows)
        {
          for (int x = 0; x < totalColumns; x++)
          {
            value = dr[x].ToString();

            if (value.Contains(",") || value.Contains("\""))
            {
              value = '"' + value.Replace("\"", "\"\"") + '"';
            }

            sb.Append(value);

            if (x != (totalColumns - 1))
            {
              sb.Append(",");
            }
          }
          sb.AppendLine();
        }
        File.WriteAllText(strFilePath, sb.ToString());
      }
    }
  }
}



