using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface ICcsSsoEmailService
  {
    Task SendUserWelcomeEmailAsync(string email, string idpName);

    Task SendOrgProfileUpdateEmailAsync(string email);

    Task SendUserProfileUpdateEmailAsync(string email);

    Task SendUserContactUpdateEmailAsync(string email);

    Task SendUserPermissionUpdateEmailAsync(string email);
  }
}
