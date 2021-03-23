using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class PagedResultSet<T> : PaginationInfo
  {
    public List<T> Results { get; set; }
  }

  public class PaginationInfo
  {
    public int CurrentPage { get; set; }

    public int PageCount { get; set; }

    public int RowCount { get; set; }
  }
}
