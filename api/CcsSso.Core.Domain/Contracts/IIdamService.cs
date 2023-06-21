using CcsSso.Domain.Dtos;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IIdamService
  {
    Task DeleteUserInIdamAsync(string userName);

    Task RegisterUserInIdamAsync(SecurityApiUserInfo securityApiUserInfo);

    Task UpdateUserMfaInIdamAsync(SecurityApiUserInfo securityApiUserInfo);

    Task ResetUserPasswordAsync(string userName);

    Task<string> GetActivationEmailVerificationLink(string email);
  }
}
