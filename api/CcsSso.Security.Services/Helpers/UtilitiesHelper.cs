using CcsSso.Security.Domain.Dtos;
using CcsSso.Shared.Domain.Constants;
using IdentityModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CcsSso.Security.Services.Helpers
{
  public static class UtilitiesHelper
  {
    /// <summary>
    /// Generates a Random Password
    /// respecting the given strength requirements.
    /// </summary>
    /// <param name="opts">A valid PasswordOptions object
    /// containing the password strength requirements.</param>
    /// <returns>A random password</returns>
    public static string GenerateRandomPassword(PasswordPolicy passwordPolicy)
    {
      string[] randomChars = new[] {
        "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
        "abcdefghijkmnopqrstuvwxyz",    // lowercase
        "0123456789",                   // digits
        "!@#$%^&*"                        // non-alphanumeric
      };
      CryptoRandom rand = new CryptoRandom();
      List<char> chars = new List<char>();

      if (passwordPolicy.LowerAndUpperCaseWithDigits)
      {
        chars.Insert(rand.Next(0, chars.Count),
            randomChars[0][rand.Next(0, randomChars[0].Length)]);

        chars.Insert(rand.Next(0, chars.Count),
            randomChars[1][rand.Next(0, randomChars[1].Length)]);

        chars.Insert(rand.Next(0, chars.Count),
            randomChars[2][rand.Next(0, randomChars[2].Length)]);

        chars.Insert(rand.Next(0, chars.Count),
            randomChars[3][rand.Next(0, randomChars[3].Length)]);
      }

      for (int i = chars.Count; i < passwordPolicy.RequiredLength
          || chars.Distinct().Count() < passwordPolicy.RequiredUniqueChars; i++)
      {
        string rcs = randomChars[rand.Next(0, randomChars.Length)];
        chars.Insert(rand.Next(0, chars.Count),
            rcs[rand.Next(0, rcs.Length)]);
      }

      return new string(chars.ToArray());
    }
  }
}
