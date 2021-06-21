using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperUserService
  {
    Task<WrapperUserResponse> GetUserAsync(string userName);

    Task<string> UpdateUserAsync(string userName, WrapperUserRequest wrapperUserRequest);
  }
}
