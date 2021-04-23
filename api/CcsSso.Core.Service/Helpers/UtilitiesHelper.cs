using CcsSso.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CcsSso.Services.Helpers
{
  public static class UtilitiesHelper
  {
    public static bool IsEmailValid(string emailaddress)
    {
      Regex regex = new Regex(RegexExpressions.VALID_EMAIL_FORMAT_REGEX);
      Match match = regex.Match(emailaddress);
      return match.Success;
    }

    public static bool IsPhoneNumberValid(string phoneNumber)
    {
      Regex regex = new Regex(RegexExpressions.VALID_PHONE_E164_FORMAT_REGEX);
      Match match = regex.Match(phoneNumber);
      return match.Success;
    }

    public static bool IsEnumValueValid<TEnum>(int value) where TEnum : struct
    {
      List<int> enumVals = Enum.GetValues(typeof(TEnum)).Cast<int>().ToList();

      var lowest = enumVals.OrderBy(i => i).First();
      var highest = enumVals.OrderByDescending(i => i).First();

      return value >= lowest && value <= highest;
    }
  }
}
