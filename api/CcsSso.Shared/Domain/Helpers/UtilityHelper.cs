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
        string emailUserNameMaxCharacters = email.Split('@')[0];
        string hostname = email.Split('@')[1];

        string[] userIdDomain_SubDomain= hostname.Split('.');
        string emaildomainNameMaxCharacters = userIdDomain_SubDomain[0];
        string emaildomainMaxCharacters = userIdDomain_SubDomain[1];

        if (email.Length > Constants.Constants.EmailMaxCharacters ||
          emailUserNameMaxCharacters.Length > Constants.Constants.EmailUserNameMaxCharacters ||
          emaildomainNameMaxCharacters.Length > Constants.Constants.EmaildomainnameNameMaxCharacters ||
          emaildomainMaxCharacters.Length > Constants.Constants.EmaildomainMaxCharacters)
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

    public static bool IsUserNameValid(string Name)
    {
      if (string.IsNullOrWhiteSpace(Name))
      {
        return false;
      }
      else
      {
        Regex regex = new Regex(RegexExpression.VALID_USER_NAME);
        Match match = regex.Match(Name);
        return match.Success;
      }
    }

    public static bool IsUserNameLengthValid(string Name)
    {
      if (string.IsNullOrWhiteSpace(Name))
      {
        return false;
      }
      else if(Name.Length<=1)
      {
        return false;
      }
      else
      {
        return true;
      }
    }
  }
}
