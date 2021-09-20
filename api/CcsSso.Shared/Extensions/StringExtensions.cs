using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CcsSso.Shared.Extensions
{
  public static class StringExtensions
  {
    public static byte[] ToByteArray(this string value) =>
               Convert.FromBase64String(value);

    public static bool IsInvalidCharactorIncluded(this string value, string regex) => Regex.IsMatch(value, regex);
  }
}
