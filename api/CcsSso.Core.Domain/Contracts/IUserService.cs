using CcsSso.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IUserService
  {
    Task<List<ServicePermissionDto>> GetPermissions(string userName, string serviceClientId);

    Task SendUserActivationEmailAsync(string email);
  }
}
