using System;

namespace CcsSso.Security.Domain.Dtos
{
  public class IdentityProviderInfoDto
  {
    public string Name { get; set; }

    public string Type { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime LastModifiedDate { get; set; }
  }
}
