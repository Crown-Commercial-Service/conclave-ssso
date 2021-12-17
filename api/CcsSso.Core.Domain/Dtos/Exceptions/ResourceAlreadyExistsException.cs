using System;

namespace CcsSso.Core.Domain.Dtos.Exceptions
{
  public class ResourceAlreadyExistsException : Exception
  {
    public ResourceAlreadyExistsException()
        : base()
    {
    }

    public ResourceAlreadyExistsException(string message)
        : base(message)
    {
    }
  }
}
