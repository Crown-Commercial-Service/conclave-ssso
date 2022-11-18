using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IUserService
  {
    Task<List<ServicePermissionDto>> GetPermissions(string userName, string serviceClientId, string organisationId);

    Task SendUserActivationEmailAsync(string email, bool isExpired = false);

    Task NominateUserAsync(string email);
  }
}
