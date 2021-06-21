using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain.Excecptions
{
  public class ResourceAlreadyExistsException : Exception
  {
    public ResourceAlreadyExistsException()
        : base()
    {
    }
  }
}
