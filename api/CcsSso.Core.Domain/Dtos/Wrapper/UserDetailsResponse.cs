using CcsSso.Core.Domain.Dtos.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.Wrapper
{
  public class UserDetailsResponse:UserProfileResponseInfo
  {
    public bool IsDormant { get; set; }
  }
}
