namespace CcsSso.Shared.Domain.Constants
{
  public static class RegexExpression
  {
    public const string INVALID_CHARACTORS_FOR_API_INPUT = @"(<)|(>)|(\/\*)|(\*\/)";
    public const string VALID_EMAIL_FORMAT_REGEX = @"^\s?([\w!#$%+&'*-/=?^_`{|}~][^,]*)@([\w\.\-]+)((\.(\w){1,1000})+)\s?$";
    public const string VALID_PHONE_E164_FORMAT_REGEX = @"^\+[1-9]\d{1,14}$";
    public const string VALID_PASSWORD_FORMAT_REGEX = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[!@#$%^&*_]).{4,}$";
    public const string VALID_USER_NAME = @"[a-zA-Z-,'.]+$";
    public const string VALID_GROUP_NAME = @"^[a-zA-Z\0-9-,.&/()+#;:@']{3,256}$";
    public const string VALID_CONTACT_NAME = @"^[a-zA-Z\0-9-.,'/&()]{3,256}$";
    public const string VALID_STREET_ADDRESS = @"^[a-zA-Z\0-9-,.&/()+#;:@']{1,256}$";
    public const string VALID_LOCALITY = @"^[a-zA-Z\0-9-.,&/']{0,256}$";
    public const string VALID_SITENAME = @"^[a-zA-Z\0-9-,.&/()+#;:@']{3,256}$";
  }
}
