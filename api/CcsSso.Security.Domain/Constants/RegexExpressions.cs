namespace CcsSso.Security.Domain.Constants
{
  public static class RegexExpressions
  {
    public const string VALID_EMAIL_FORMAT_REGEX = @"^\s?[\w]([\w\.\+\-]*)@([\w\.\-]+)((\.(\w){2,4})+)\s?$";
    public const string VALID_PASSWORD_FORMAT_REGEX = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[!@#$%^&*_]).{4,}$";
  }
}
