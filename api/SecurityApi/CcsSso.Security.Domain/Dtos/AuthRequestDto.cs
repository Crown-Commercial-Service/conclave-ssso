using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Security.Domain.Dtos
{
  public class AuthRequestDto
  {
    public string UserName { get; set; }

    public string UserPassword { get; set; }
  }
}
