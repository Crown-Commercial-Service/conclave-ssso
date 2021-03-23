using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{

  public class VaultOptions
  {
    public string Address { get; set; }
  }

  public class VCapSettings
  {
    public Credentials credentials { get; set; }
  }

  public class Credentials
  {
    public string address { get; set; }
    public Auth auth { get; set; }
  }

  public class Auth
  {
    public string accessor { get; set; }
    public string token { get; set; }
  }
}

