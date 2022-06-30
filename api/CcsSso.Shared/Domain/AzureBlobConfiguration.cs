using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Domain
{
  public class AzureBlobConfiguration
  {
    public string EndpointProtocol { get; set; }
    public string AccountName  { get; set; }
    public string AccountKey { get; set; }
    public string EndpointAzure { get; set; }
    public string AzureBlobContainer { get; set; }
    public string Fileheader { get; set; }
    public string FileExtension { get; set; }
    public string FilePathPrefix { get; set; }
     
  }
}
