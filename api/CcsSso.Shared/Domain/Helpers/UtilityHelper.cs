using CcsSso.Shared.Domain.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        string[] tokens = { "*", "%", "!","$"}; 
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
        string[] tokens = { "+", "*", "%", "#", "!", "$"};
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
        string[] tokens = { "*", "%", "!", "$"};
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
        string[] tokens = { "*", "%", "!", "$"};
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
        string[] tokens = { "*", "%", "!", "$"};
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

    public static string GetDatbaseConnectionString(string name, string connectionString)
    {
      string env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var envData = (JObject)JsonConvert.DeserializeObject(env);
      string setting = JsonConvert.SerializeObject(envData["postgres"].FirstOrDefault(obj => obj["name"].Value<string>() == name));
      var postgresSettings = JsonConvert.DeserializeObject<PostgresSettings>(setting.ToString());

      connectionString = connectionString.Replace("[Server]", postgresSettings.credentials.host);
      connectionString = connectionString.Replace("[Port]", postgresSettings.credentials.port);
      connectionString = connectionString.Replace("[Database]", postgresSettings.credentials.name);
      connectionString = connectionString.Replace("[Username]", postgresSettings.credentials.username);
      connectionString = connectionString.Replace("[Password]", postgresSettings.credentials.password);

      return connectionString;
    }

    public static string GetRedisCacheConnectionString(string name, string connectionString)
    {
      string env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var envData = (JObject)JsonConvert.DeserializeObject(env);
      string setting = JsonConvert.SerializeObject(envData["redis"].FirstOrDefault(obj => obj["name"].Value<string>() == name));
      var redisCacheSettings = JsonConvert.DeserializeObject<RedisCacheSettings>(setting.ToString());

      connectionString = connectionString.Replace("[Host]", redisCacheSettings.credentials.host);
      connectionString = connectionString.Replace("[Port]", redisCacheSettings.credentials.port);
      connectionString = connectionString.Replace("[Password]", redisCacheSettings.credentials.password);

      return connectionString;
    }

    public static S3Settings GetS3Settings(string name)
    {
      string env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var envData = (JObject)JsonConvert.DeserializeObject(env);
      string setting = JsonConvert.SerializeObject(envData["aws-s3-bucket"].FirstOrDefault(obj => obj["name"].Value<string>() == name));
      var settings = JsonConvert.DeserializeObject<S3Settings>(setting.ToString());
      return settings;
    }

    public static SqsSetting GetSqsSetting(string name)
    {
      string env = Environment.GetEnvironmentVariable("VCAP_SERVICES", EnvironmentVariableTarget.Process);
      var envData = (JObject)JsonConvert.DeserializeObject(env);
      string setting = JsonConvert.SerializeObject(envData["aws-sqs-queue"].FirstOrDefault(obj => obj["name"].Value<string>() == name));
      var settings = JsonConvert.DeserializeObject<SqsSetting>(setting.ToString());
      return settings;
    }

    public static List<T> GetPagedResult<T>(List<T> list, int currentPage, int pageSize, out int pageCount)
    {
      var pCount = (double)list.Count / pageSize;
      pageCount = (int)Math.Ceiling(pCount);

      var skip = (currentPage - 1) * pageSize;
      return list.Skip(skip).Take(pageSize).ToList();
    }

  }
}
