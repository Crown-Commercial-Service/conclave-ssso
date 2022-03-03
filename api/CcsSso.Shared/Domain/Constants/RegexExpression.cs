namespace CcsSso.Shared.Domain.Constants
{
  public static class RegexExpression
  {
    public const string INVALID_CHARACTORS_FOR_API_INPUT = @"(<)|(>)|(\/\*)|(\*\/)";
    public const string VALID_EMAIL_FORMAT_REGEX = @"^\s?[\w]([\w\.\+\-]*)@([\w\.\-]+)((\.(\w){4,256})+)\s?$";
    public const string VALID_PHONE_E164_FORMAT_REGEX = @"^\+[1-9]\d{1,14}$";
    public const string VALID_PASSWORD_FORMAT_REGEX = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[!@#$%^&*_]).{4,}$";
  }
}
