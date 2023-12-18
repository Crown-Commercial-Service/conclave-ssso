using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class UserDataFilterCriteria
  {
    public string? UserName { get; set; } = null;

    public string? IncludeCiiOrganisationIds { get; set; }

    public string? ExcludeCiiOrganisationIds { get; set; }

    public int PageSize { get; set; }

    public int CurrentPage { get; set; }

    public bool IsPagination { get; set; } = true;

    public bool IsDormantedUsers { get; set; } = false;
  }
}
