using System;

namespace CcsSso.Domain.Exceptions
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
