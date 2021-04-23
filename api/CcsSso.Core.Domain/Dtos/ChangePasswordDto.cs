namespace CcsSso.Core.Domain.Dtos
{
  public class ChangePasswordDto
  {
    public string UserName { get; set; }

    public string NewPassword { get; set; }

    public string OldPassword { get; set; }
  }
}
