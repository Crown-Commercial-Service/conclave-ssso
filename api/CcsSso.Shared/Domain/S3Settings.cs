using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class S3Settings
  {
    public S3SettingsCredentials credentials { get; set; }
  }

  public class S3SettingsCredentials
  {
    public string aws_access_key_id { get; set; }
    public string aws_region { get; set; }
    public string aws_secret_access_key { get; set; }
    public string bucket_name { get; set; }
    public string deploy_env { get; set; }
  }
}

