using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IAuthService
  {
    Task<bool> ValidateBackChannelLogoutTokenAsync(string backChanelLogoutToken);
  }
}
