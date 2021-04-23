using CcsSso.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IUserService
  {
    Task<string> CreateAsync(UserDto model);

    Task DeleteAsync(int id);

    Task<UserDetails> GetAsync(int id);

    Task<UserDetails> GetAsync(string userName);

    Task<List<ServicePermissionDto>> GetPermissions();
  }
}
