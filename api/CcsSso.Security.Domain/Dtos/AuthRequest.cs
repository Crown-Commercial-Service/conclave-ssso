using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CcsSso.Security.Domain.Dtos
{
  public class AuthRequest
  {
    [Required]
    public string UserName { get; set; }

    [Required]
    public string UserPassword { get; set; }
  }
}
