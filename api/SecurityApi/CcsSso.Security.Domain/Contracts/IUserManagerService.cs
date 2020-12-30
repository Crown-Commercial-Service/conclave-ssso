using CcsSso.Security.Domain.Dtos;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface IUserManagerService
  {
    Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo);

    Task UpdateUserAsync(UserInfo userInfo);
  }
}
