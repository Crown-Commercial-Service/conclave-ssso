using CcsSso.Shared.Domain;
using System.Threading.Tasks;

namespace CcsSso.Shared.Contracts
{
  public interface IEmailProviderService
  {
    Task SendEmailAsync(EmailInfo emailInfo);
  }
}
