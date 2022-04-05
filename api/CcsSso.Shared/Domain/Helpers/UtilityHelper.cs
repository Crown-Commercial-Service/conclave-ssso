using CcsSso.Shared.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CcsSso.Shared.Domain.Helpers
{
  public static class UtilityHelper
  {
    public static bool IsEmailFormatValid(string emailaddress)
    {
      if (string.IsNullOrWhiteSpace(emailaddress))
      {
        return false;
      }
      else
      {
        Regex regex = new Regex(RegexExpression.VALID_EMAIL_FORMAT_REGEX);
        Match match = regex.Match(emailaddress);
        return match.Success;
      }
    }

    public static bool IsEmailLengthValid(string email)
    {
     try
      {
        string[] userIdDomain_SubDomain = email.Split('.');
        string EmaildomainMaxCharacters = userIdDomain_SubDomain[1];
        string[] userId_Domain = userIdDomain_SubDomain[0].Split('@');
        string EmailUserNameMaxCharacters = userId_Domain[0];
        string EmaildomainnameNameMaxCharacters = userId_Domain[1];

        if (email.Length > Constants.Constants.EmailMaxCharacters ||
          EmailUserNameMaxCharacters.Length > Constants.Constants.EmailUserNameMaxCharacters ||
          EmaildomainnameNameMaxCharacters.Length > Constants.Constants.EmaildomainnameNameMaxCharacters ||
          EmaildomainMaxCharacters.Length > Constants.Constants.EmaildomainMaxCharacters)
        {
          return false;
        }
        return true;
      }
      catch(Exception ex)
      {
        return false;
      }
    }

      public static bool IsPhoneNumberValid(string phoneNumber)
    {
      Regex regex = new Regex(RegexExpression.VALID_PHONE_E164_FORMAT_REGEX);
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

    public static bool IsPasswordValidForRequiredCharactors(string password)
    {
      Regex regex = new Regex(RegexExpression.VALID_PASSWORD_FORMAT_REGEX);
      Match match = regex.Match(password);
      return match.Success;
    }
  }
}
