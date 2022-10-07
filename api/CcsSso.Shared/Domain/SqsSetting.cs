using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class SqsSetting
  {
    public SqsSettingCredentials credentials { get; set; }
  }

  public class SqsSettingCredentials
  {
    public string aws_access_key_id { get; set; }
    public string aws_region { get; set; }
    public string aws_secret_access_key { get; set; }
    public string primary_queue_url { get; set; }
    public string secondary_queue_url { get; set; }
  }
}

