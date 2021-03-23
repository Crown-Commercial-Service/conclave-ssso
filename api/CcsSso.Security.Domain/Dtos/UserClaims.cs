using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Security.Domain.Dtos
{
  public class UserClaims
  {
    public string UserName { get; set; }

    public List<AttributeType> Claims { get; set; }

    public class AttributeType
    {
      public string Name { get; set; }

      public string Value { get; set; }
    }
  }
}
