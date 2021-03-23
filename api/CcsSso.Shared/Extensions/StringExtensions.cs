using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Extensions
{
  public static class StringExtensions
  {
    public static byte[] ToByteArray(this string value) =>
               Convert.FromBase64String(value);
  }
}
