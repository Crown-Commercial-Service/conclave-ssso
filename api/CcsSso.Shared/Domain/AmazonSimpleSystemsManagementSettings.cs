using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class AmazonSimpleSystemsManagementSettings
  {
    public AmazonSimpleSystemsManagementCredentials credentials { get; set; }
  }

  public class AmazonSimpleSystemsManagementCredentials
  {
    public string aws_access_key_id { get; set; }
    public string aws_secret_access_key { get; set; }
    public string region { get; set; }
  }
}

