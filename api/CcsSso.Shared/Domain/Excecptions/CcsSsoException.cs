using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain.Excecptions
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
