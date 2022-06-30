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

        string[] userIdDomain_SubDomain = hostname.Split('.');
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
      catch (Exception ex)
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
      bool IsRestrictedChar = false;
      try
      {
        string[] tokens = "+ * % # ! $".Split();
        IsRestrictedChar = tokens.Any(t => Name.Contains(t));

        if (string.IsNullOrWhiteSpace(Name))
        {
          return false;
        }
        else if (IsRestrictedChar == true)
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
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool IsUserNameLengthValid(string Name)
    {
      try
      {
        return Name.Length < 1 ? false : true;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool IsGroupNameValid(string Name)
    {
      bool IsRestrictedChar = false;
      try
      {
        string[] tokens = { "*", "%", "!","$" }; 
        IsRestrictedChar = tokens.Any(t => Name.Contains(t));
        if (IsRestrictedChar == true)
        {
          return false;
        }
        else
        {
          Regex regex = new Regex(RegexExpression.VALID_GROUP_NAME);
          Match match = regex.Match(Name);
          return match.Success;
        }
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool IsContactPointNameValid(string Name)
    {
      bool IsRestrictedChar = false;
      try
      {
        string[] tokens = { "+", "*", "%", "#", "!", "$" };
        IsRestrictedChar = tokens.Any(t => Name.Contains(t));
        if (IsRestrictedChar == true)
        {
          return false;
        }
        else
        {
          Regex regex = new Regex(RegexExpression.VALID_CONTACT_NAME);
          Match match = regex.Match(Name);
          return match.Success;
        }
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool IsStreetAddressValid(string Name)
    {
      bool IsRestrictedChar = false;
      try
      {
        string[] tokens = { "*", "%", "!", "$" };
        IsRestrictedChar = tokens.Any(t => Name.Contains(t));
        if (IsRestrictedChar == true)
        {
          return false;
        }
        else
        {
          Regex regex = new Regex(RegexExpression.VALID_STREET_ADDRESS);
          Match match = regex.Match(Name);
          return match.Success;
        }
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool IslocalityValid(string Name)
    {
      bool IsRestrictedChar = false;
      try
      {
        string[] tokens = { "*", "%", "!", "$" };
        IsRestrictedChar = tokens.Any(t => Name.Contains(t));
        if (IsRestrictedChar == true)
        {
          return false;
        }
        else
        {
          Regex regex = new Regex(RegexExpression.VALID_LOCALITY);
          Match match = regex.Match(Name);
          return match.Success;
        }
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool IsSiteNameValid(string Name)
    {
      bool IsRestrictedChar = false;
      try
      {
        string[] tokens = { "*", "%", "!", "$" };
        IsRestrictedChar = tokens.Any(t => Name.Contains(t));
        if (IsRestrictedChar == true)
        {
          return false;
        }
        else
        {
          Regex regex = new Regex(RegexExpression.VALID_SITENAME);
          Match match = regex.Match(Name);
          return match.Success;
        }
      }
      catch (Exception ex)
      {
        return false;
      }
    }
  }
}
