using System;

namespace CcsSso.Security.Domain.Exceptions
{
  public class CcsSsoException : Exception
  {
    public CcsSsoException()
            : base()
    {
    }

    public CcsSsoException(string errorCode)
        : base(errorCode)
    {
    }
  }
}
