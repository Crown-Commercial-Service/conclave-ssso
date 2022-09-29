using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class RedisCacheSettings
  {
    public RedisCacheSettingsCredentials credentials { get; set; }
  }

  public class RedisCacheSettingsCredentials
  {
    public string host { get; set; }
    public string password { get; set; }
    public string port { get; set; }
    public string username { get; set; }
  }
}

