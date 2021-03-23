namespace CcsSso.Domain.Constants
{
  public static class RegexExpressions
  {
    public const string VALID_EMAIL_FORMAT_REGEX = @"^\s?[\w]([\w\.\+\-]*)@([\w\.\-]+)((\.(\w){2,4})+)\s?$";
    public const string VALID_PHONE_E164_FORMAT_REGEX = @"^\+[1-9]\d{1,14}$";
  }
}
