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

    public string DataMigrationBucketName { get; set; }

    public string DataMigrationTemplateFolderName { get; set; }

    public string DataMigrationFolderName { get; set; }

    public int FileAccessExpirationInHours { get; set; }
  }
}
