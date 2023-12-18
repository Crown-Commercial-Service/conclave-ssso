using CcsSso.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.Wrapper
{
  public class UserDataResponseInfo : PaginationInfo
  {
    public List<UserDataDto> UserList { get; set; }
  }

  public class UserDataDto
  {
    public string UserName { get; set; }
    public string Name { get; set; }
    public string OrganisationId { get; set; }
    public List<int> Roles { get; set; }
    public List<int> Groups { get; set; }
    public bool IsDormant { get; set; }
    public DormantBy? DormantBy { get; set; }
    public DateTime? DormantedOnUtc { get; set; }
  }
}
