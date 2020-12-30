using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Security.Domain.Dtos
{
  public class UserInfo
  {
    public string UserName { get; set; }

    public string Email { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Role { get; set; }

    public List<string> Groups { get; set; }
  }
}
