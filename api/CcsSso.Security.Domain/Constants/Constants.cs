using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Security.Domain.Constants
{
  public  static class Constants
  {
    public static class ErrorCodes
    {
      public const string FirstNameRequired = "ERROR_FIRSTNAME_REQUIRED";
      public const string UserIdRequired = "ERROR_USERID_REQUIRED";
      public const string LastNameRequired = "ERROR_LASTNAME_REQUIRED";
      public const string EmailRequired = "ERROR_EMAIL_REQUIRED";
      public const string EmailFormatError = "ERROR_EMAIL_FORMAT";
      public const string EmailTooLongError = "ERROR_EMAIL_TOO_LONG";
      public const string InvalidTicket = "INVALID_TICKET";
      public const string MaxPasswordResetAttempt = "ERROR_MAX_ATTEMPT";
    }

    public static class CacheKey
    {
      public const string MFA_RESET = "MFA_RESET";
      public const string MFA_RESET_PERSISTENT = "MFA_RESET_PERSISTENT";
      // #Delegated
      public const string DELEGATION = "DELEGATION";
    }
  }
}
