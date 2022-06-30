namespace CcsSso.Shared.Services
{
  public interface ICSVConverter
  {
    public byte[] ConvertToCSV(dynamic jsonArrayObject, string filetype);
  }
}