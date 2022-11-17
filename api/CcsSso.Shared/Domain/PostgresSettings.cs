using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class PostgresSettings
  {
    public PostgresCredentials credentials { get; set; }
  }

  public class PostgresCredentials
  {
    public string host { get; set; }
    public string name { get; set; }
    public string password { get; set; }
    public string port { get; set; }
    public string username { get; set; }
  }
}

