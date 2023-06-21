using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Domain
{
  public class S3ConfigurationInfo
  {
    public string ServiceUrl { get; set; }

    public string AccessKeyId { get; set; }

    public string AccessSecretKey { get; set; }

    public string BulkUploadBucketName { get; set; }

    public string BulkUploadTemplateFolderName { get; set; }

    public string BulkUploadFolderName { get; set; }

    public int FileAccessExpirationInHours { get; set; }
  }
}
